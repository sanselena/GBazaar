using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                .FirstOrDefault(x => x.PRID == id);

            if (pr == null)
                return NotFound();

            // Approval rules'ı bul
            var firstRule = _context.ApprovalRules
                .Where(r => pr.EstimatedTotal >= r.MinAmount &&
                            (r.MaxAmount == null || pr.EstimatedTotal <= r.MaxAmount))
                .OrderBy(r => r.ApprovalLevel)
                .FirstOrDefault();

            if (firstRule == null)
                return BadRequest("No approval rule found for this amount.");

            // PR durumunu güncelle
            pr.PRStatus = PRStatusType.PendingApproval;
            pr.PRStatusID = (int)PRStatusType.PendingApproval;

            // İlk level: history'ye "Submitted"
            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                PRID = pr.PRID,
                ApproverID = firstRule.RequiredRoleID, // sadece role tutuyorsun
                ActionDate = DateTime.Now,
                ActionType = ApprovalActionType.Forwarded,
                ApprovalLevel = firstRule.ApprovalLevel,
                Notes = "Approval chain started."
            });

            _context.SaveChanges();
            return RedirectToAction("Details", "PR", new { id = pr.PRID });
        }

        
        public IActionResult Approve(int id)
        {
            // Mevcut User → Approver
            int approverId = 1; // şimdilik login yok, hepsini 1 yapabiliriz
            var approver = _context.Users.FirstOrDefault(u => u.UserID == approverId);
            if (approver == null)
                return BadRequest("Approver user not found.");

            var pr = _context.PurchaseRequests
                .FirstOrDefault(x => x.PRID == id);

            if (pr == null)
                return NotFound();

            // PR'ın totaline göre geçerli approval rules
            var rules = _context.ApprovalRules
                .Where(r => pr.EstimatedTotal >= r.MinAmount &&
                            (r.MaxAmount == null || pr.EstimatedTotal <= r.MaxAmount))
                .OrderBy(r => r.ApprovalLevel)
                .ToList();

            if (!rules.Any())
                return BadRequest("No approval chain found.");

            // Şu an hangi role onay veriyor?
            var currentRule = rules.FirstOrDefault(r => r.RequiredRoleID == approver.RoleID);

            if (currentRule == null)
            {
                return BadRequest("You are not allowed to approve this PR.");
            }

            // HISTORY: Approved ekle
            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                PRID = pr.PRID,
                ApproverID = approver.UserID,
                ActionDate = DateTime.Now,
                ActionType = ApprovalActionType.Approved,
                ApprovalLevel = currentRule.ApprovalLevel,
                Notes = "Approved"
            });

            // Bir sonraki level var mı?
            var nextRule = rules
                .Where(r => r.ApprovalLevel > currentRule.ApprovalLevel)
                .OrderBy(r => r.ApprovalLevel)
                .FirstOrDefault();

            if (nextRule != null)
            {
                // Sıradaki kişiye pasla
                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    PRID = pr.PRID,
                    ApproverID = nextRule.RequiredRoleID,
                    ActionDate = DateTime.Now,
                    ActionType = ApprovalActionType.Forwarded,
                    ApprovalLevel = nextRule.ApprovalLevel,
                    Notes = "Forwarded to next approval level."
                });
            }
            else
            {
                // SON LEVEL → PR TAMAMEN ONAYLANDI
                pr.PRStatus = PRStatusType.Approved;
                pr.PRStatusID = (int)PRStatusType.Approved;
            }

            _context.SaveChanges();
            return RedirectToAction("Details", "PR", new { id = pr.PRID });
        }


        // -------------------------------------------------------------
        // 3) Reject
        // -------------------------------------------------------------
        public IActionResult Reject(int id, string notes)
        {
            int approverId = 1;
            var approver = _context.Users.FirstOrDefault(u => u.UserID == approverId);

            var pr = _context.PurchaseRequests.Find(id);
            if (pr == null) return NotFound();

            pr.PRStatus = PRStatusType.Rejected;
            pr.PRStatusID = (int)PRStatusType.Rejected;

            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                PRID = id,
                ApproverID = approver.UserID,
                ActionDate = DateTime.Now,
                ActionType = ApprovalActionType.Rejected,
                ApprovalLevel = 0,
                Notes = notes
            });

            _context.SaveChanges();
            return RedirectToAction("Details", "PR", new { id });
        }

        // -------------------------------------------------------------
        // 4) Approval History görüntüleme
        // -------------------------------------------------------------
        public IActionResult History(int id)
        {
            var history = _context.ApprovalHistories
                .Include(h => h.Approver)
                .Where(h => h.PRID == id)
                .OrderBy(h => h.ActionDate)
                .ToList();

            return View(history);
        }
    }
}