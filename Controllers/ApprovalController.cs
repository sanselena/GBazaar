using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GBazaar.ViewModels.Approval;

namespace GBazaar.Controllers
{
    public class ApprovalController : Controller
    {
        private readonly ProcurementContext _context;

        public ApprovalController(ProcurementContext context)
        {
            _context = context;
        }

        public IActionResult Submit(int id)
        {
            var pr = _context.PurchaseRequests
                .Include(x => x.PRItems)
                .Include(x => x.Requester)
                .FirstOrDefault(x => x.PRID == id);

            if (pr == null)
                return NotFound();

            var requestor = pr.Requester;

            if (requestor == null)
                return BadRequest("Requestor user not found.");

            // reqin tutarına göre rule bul
            var applicableRules = _context.ApprovalRules
                .Where(r => pr.EstimatedTotal >= r.MinAmount &&
                            (r.MaxAmount == null || pr.EstimatedTotal <= r.MaxAmount))
                .OrderBy(r => r.ApprovalLevel)
                .ToList();

            if (!applicableRules.Any())
                return BadRequest("No approval rule found for this amount.");

            // ilk app. rule
            var firstRule = applicableRules.First();

            // ruleı onaylayacak userı-n rolünü bul
            int assignedUserId = GetUserForRole(requestor.UserID, firstRule.RequiredRoleID);

            if (assignedUserId == 0)
                return BadRequest($"No user found for the required role (RoleID: {firstRule.RequiredRoleID}) in this department.");

            // pr statusunu pending 
            pr.PRStatus = PRStatusType.PendingApproval;

            // ilk approval history kaydını oluştur
            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                PRID = pr.PRID,
                ApproverID = assignedUserId,
                ActionType = ApprovalActionType.Forwarded,
                ApprovalLevel = firstRule.ApprovalLevel,
                ActionDate = DateTime.Now,
                Notes = $"Approval chain started. Forwarded to {firstRule.RequiredRole?.RoleName ?? "Unknown Role"}."
            });

            _context.SaveChanges();

            return RedirectToAction("Profile", "Buyer");
        }

        public IActionResult Approve(int id)
        {
            // loggedin userın idsini çek
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var approverId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var pr = _context.PurchaseRequests
                .Include(x => x.Requester)
                .Include(x => x.PRItems) 
                .FirstOrDefault(x => x.PRID == id);

            if (pr == null)
                return NotFound();

            var requestor = pr.Requester;

            // son approval adımını bul
            var lastStep = _context.ApprovalHistories
                .Where(h => h.PRID == id)
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            if (lastStep == null)
                return BadRequest("Approval chain not started.");

            if (lastStep.ApproverID != approverId)
                return BadRequest("This PR is not assigned to you.");

            // tutara göre app chain çek
            var applicableRules = _context.ApprovalRules
                .Include(r => r.RequiredRole)
                .Where(r => pr.EstimatedTotal >= r.MinAmount &&
                            (r.MaxAmount == null || pr.EstimatedTotal <= r.MaxAmount))
                .OrderBy(r => r.ApprovalLevel)
                .ToList();

            var currentRule = applicableRules.FirstOrDefault(r => r.ApprovalLevel == lastStep.ApprovalLevel);

            if (currentRule == null)
                return BadRequest("Current approval rule not found.");

            // mevcut level için onay kaydını ekle
            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                PRID = id,
                ApproverID = approverId,
                ActionType = ApprovalActionType.Approved,
                ApprovalLevel = currentRule.ApprovalLevel,
                ActionDate = DateTime.Now,
                Notes = $"Approved by {currentRule.RequiredRole?.RoleName ?? "Unknown Role"}."
            });

            // sonraki leveli bul
            var nextRule = applicableRules.FirstOrDefault(r => r.ApprovalLevel > currentRule.ApprovalLevel);

            if (nextRule == null)
            {
                // Son gerekli onay alındıysa pr approved
                pr.PRStatus = PRStatusType.Approved;

                // poya geçiş
                CreatePurchaseOrder(pr);
            }
            else
            {
                // sonraki onaylayıcı
                int nextUserId = GetUserForRole(requestor.UserID, nextRule.RequiredRoleID);

                if (nextUserId == 0)
                    return BadRequest($"No user found for next approval role (RoleID: {nextRule.RequiredRoleID}) in this department.");

                // ileri lvle at
                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    PRID = id,
                    ApproverID = nextUserId,
                    ActionType = ApprovalActionType.Forwarded,
                    ApprovalLevel = nextRule.ApprovalLevel,
                    ActionDate = DateTime.Now,
                    Notes = $"Forwarded to {nextRule.RequiredRole?.RoleName ?? "Unknown Role"} for approval."
                });
            }

            _context.SaveChanges();

           
            return RedirectToAction("Profile", "Buyer");
        }

        public IActionResult Reject(int id, string notes)
        {
            // logged in user id çek
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var approverId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var pr = _context.PurchaseRequests
                .Include(x => x.Requester)
                .FirstOrDefault(x => x.PRID == id);

            if (pr == null)
                return NotFound();

            var lastStep = _context.ApprovalHistories
                .Where(h => h.PRID == id)
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            if (lastStep == null)
                return BadRequest("Approval chain not started.");

            if (lastStep.ApproverID != approverId)
                return BadRequest("This PR is not assigned to you.");

            // prı reject et
            pr.PRStatus = PRStatusType.Rejected;

            
            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                PRID = id,
                ApproverID = approverId,
                ActionType = ApprovalActionType.Rejected,
                ApprovalLevel = lastStep.ApprovalLevel,
                ActionDate = DateTime.Now,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Rejected without specific reason." : notes
            });

            _context.SaveChanges();

        
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            return userRole switch
            {
                "2" => RedirectToAction("Dashboard", "Manager"),
                "3" => RedirectToAction("Dashboard", "Director"),
                "4" => RedirectToAction("Dashboard", "CFO"),
                _ => RedirectToAction("Profile", "Buyer")
            };
        }

        public IActionResult History(int id)
        {
            var history = _context.ApprovalHistories
                .Include(h => h.Approver)
                .Where(h => h.PRID == id)
                .OrderBy(h => h.ActionDate)
                .ToList();

            return View(history);
        }

        private int GetUserForRole(int requestorUserId, int roleId)
        {
            var requestor = _context.Users
                .Include(u => u.Department)
                .FirstOrDefault(u => u.UserID == requestorUserId);

            if (requestor?.DepartmentID == null)
                return 0;

            int departmentId = requestor.DepartmentID.Value;

            // aynı departmandaki bu role sahip kullanıcıyı bul
            return _context.Users
                .Where(u => u.RoleID == roleId && u.DepartmentID == departmentId && u.IsActive)
                .Select(u => u.UserID)
                .FirstOrDefault();
        }

        // po oluşturma
        private void CreatePurchaseOrder(PurchaseRequest pr)
        {
            var po = new PurchaseOrder
            {
                PRID = pr.PRID,
                SupplierID = pr.SupplierID ?? throw new InvalidOperationException("Supplier ID is required"),
                DateIssued = DateTime.UtcNow,

                // po suptan onay bekler
                POStatus = POStatusType.PendingSupplierApproval,
                POStatusID = (int)POStatusType.PendingSupplierApproval,

                RequiredDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))
            };

            // poitemleri çek
            foreach (var prItem in pr.PRItems)
            {
                var poItem = new POItem
                {
                    PurchaseOrder = po,
                    ProductID = prItem.ProductID,
                    ItemName = prItem.PRItemName,
                    Description = prItem.Description,
                    QuantityOrdered = (int)prItem.Quantity,
                    UnitPrice = prItem.UnitPrice ?? 0
                };
                po.POItems.Add(poItem);
            }

            _context.PurchaseOrders.Add(po);

            // prı awaiting sup yap
            pr.PRStatus = PRStatusType.AwaitingSupplier;
        }
    }

  
}