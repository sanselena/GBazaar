using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

            // Purchase Request'in tutarına göre hangi approval level'dan başlayacağını belirle
            var applicableRules = _context.ApprovalRules
                .Where(r => pr.EstimatedTotal >= r.MinAmount &&
                            (r.MaxAmount == null || pr.EstimatedTotal <= r.MaxAmount))
                .OrderBy(r => r.ApprovalLevel)
                .ToList();

            if (!applicableRules.Any())
                return BadRequest("No approval rule found for this amount.");

            // İlk approval level'ı belirle
            var firstRule = applicableRules.First();

            // Requestor'ın departmanında bu role sahip kullanıcıyı bul
            int assignedUserId = GetUserForRole(requestor.UserID, firstRule.RequiredRoleID);

            if (assignedUserId == 0)
                return BadRequest($"No user found for the required role (RoleID: {firstRule.RequiredRoleID}) in this department.");

            // PR'ı PendingApproval durumuna getir
            pr.PRStatus = PRStatusType.PendingApproval;

            // İlk approval history kaydını oluştur
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
            // Giriş yapmış kullanıcının ID'sini al
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var approverId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var pr = _context.PurchaseRequests
                .Include(x => x.Requester)
                .FirstOrDefault(x => x.PRID == id);

            if (pr == null)
                return NotFound();

            var requestor = pr.Requester;

            // Son approval adımını bul
            var lastStep = _context.ApprovalHistories
                .Where(h => h.PRID == id)
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            if (lastStep == null)
                return BadRequest("Approval chain not started.");

            if (lastStep.ApproverID != approverId)
                return BadRequest("This PR is not assigned to you.");

            // Bu tutar için gerekli tüm approval rule'larını getir
            var applicableRules = _context.ApprovalRules
                .Include(r => r.RequiredRole)
                .Where(r => pr.EstimatedTotal >= r.MinAmount &&
                            (r.MaxAmount == null || pr.EstimatedTotal <= r.MaxAmount))
                .OrderBy(r => r.ApprovalLevel)
                .ToList();

            var currentRule = applicableRules.FirstOrDefault(r => r.ApprovalLevel == lastStep.ApprovalLevel);

            if (currentRule == null)
                return BadRequest("Current approval rule not found.");

            // Mevcut level için onay kaydını ekle
            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                PRID = id,
                ApproverID = approverId,
                ActionType = ApprovalActionType.Approved,
                ApprovalLevel = currentRule.ApprovalLevel,
                ActionDate = DateTime.Now,
                Notes = $"Approved by {currentRule.RequiredRole?.RoleName ?? "Unknown Role"}."
            });

            // Bir sonraki approval level'ını bul
            var nextRule = applicableRules.FirstOrDefault(r => r.ApprovalLevel > currentRule.ApprovalLevel);

            if (nextRule == null)
            {
                // Tüm onaylar tamamlandı, PR'ı Approved durumuna getir
                pr.PRStatus = PRStatusType.Approved;

                // PO oluşturma işlemi (opsiyonel)
                CreatePurchaseOrder(pr);
            }
            else
            {
                // Bir sonraki onaylayıcıyı bul
                int nextUserId = GetUserForRole(requestor.UserID, nextRule.RequiredRoleID);

                if (nextUserId == 0)
                    return BadRequest($"No user found for next approval role (RoleID: {nextRule.RequiredRoleID}) in this department.");

                // Bir sonraki level'a forward et
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

            // Kullanıcının rolüne göre geri yönlendir
            return RedirectToAction("Profile", "Buyer");
        }

        public IActionResult Reject(int id, string notes)
        {
            // Giriş yapmış kullanıcının ID'sini al
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

            // PR'ı Rejected durumuna getir
            pr.PRStatus = PRStatusType.Rejected;

            // Ret kaydını ekle
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

            // Kullanıcının rolüne göre geri yönlendir
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

            // Aynı departmandaki bu role sahip kullanıcıyı bul
            return _context.Users
                .Where(u => u.RoleID == roleId && u.DepartmentID == departmentId && u.IsActive)
                .Select(u => u.UserID)
                .FirstOrDefault();
        }

        // Purchase Order oluşturma metodu (opsiyonel, ileride kullanılabilir)
        private void CreatePurchaseOrder(PurchaseRequest pr)
        {
            var po = new PurchaseOrder
            {
                PRID = pr.PRID,
                SupplierID = pr.SupplierID ?? throw new InvalidOperationException("Supplier ID is required"),
                DateIssued = DateTime.UtcNow,
                POStatus = Models.Enums.POStatusType.Issued,
                POStatusID = (int)Models.Enums.POStatusType.Issued
            };

            _context.PurchaseOrders.Add(po);
            pr.PRStatus = PRStatusType.Ordered;
        }
    }
}