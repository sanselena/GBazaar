using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.Enums;
using GBazaar.ViewModels.Buyer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GBazaar.Controllers
{
    [Authorize]
    public class BuyerController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly ILogger<BuyerController> _logger;

        public BuyerController(ProcurementContext context, ILogger<BuyerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Buyer/Profile - Tüm roller için tek profile
        public async Task<IActionResult> Profile()
        {
            // Giriş yapmış kullanıcının ID ve Role bilgisini al
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!int.TryParse(User.FindFirst(ClaimTypes.Role)?.Value, out var userRoleId))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Department)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    TempData["ProfileError"] = "User record not found.";
                    return View(CreateSampleProfile(userRoleId));
                }

                // Role'e göre ViewBag ayarla
                SetViewBagByRole(userRoleId, user);

                // Role'e göre veri getir
                var model = await GetProfileDataByRole(userId, userRoleId, user);

                return View(model);
            }
            catch (Exception ex) when (IsProfileConnectivityIssue(ex))
            {
                _logger.LogWarning(ex, "Falling back to sample data due to connectivity issues.");
                TempData["ProfileError"] = "We couldn't reach the procurement database, showing sample data instead.";
                return View(CreateSampleProfile(userRoleId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profile for user {UserId} with role {RoleId}", userId, userRoleId);
                TempData["ProfileError"] = "Something went wrong while loading your profile. Showing sample data instead.";
                return View(CreateSampleProfile(userRoleId));
            }
        }

        private void SetViewBagByRole(int roleId, User user)
        {
            switch (roleId)
            {
                case 1: // Officer
                    ViewBag.UserType = "Buyer";
                    ViewBag.UserName = user.FullName;
                    ViewBag.DepartmentName = user.Department?.DepartmentName ?? "Unknown Department";
                    break;
                case 2: // Manager
                    ViewBag.UserType = "Manager";
                    ViewBag.UserName = user.FullName;
                    ViewBag.DepartmentName = user.Department?.DepartmentName ?? "Unknown Department";
                    break;
                case 3: // Director
                    ViewBag.UserType = "Director";
                    ViewBag.UserName = user.FullName;
                    ViewBag.DepartmentName = user.Department?.DepartmentName ?? "Executive";
                    break;
                case 4: // CFO
                    ViewBag.UserType = "CFO";
                    ViewBag.UserName = user.FullName;
                    ViewBag.DepartmentName = "Finance & Operations";
                    break;
                default:
                    ViewBag.UserType = "Buyer";
                    ViewBag.UserName = user.FullName;
                    ViewBag.DepartmentName = user.Department?.DepartmentName ?? "Unknown Department";
                    break;
            }
        }

        private async Task<BuyerProfileViewModel> GetProfileDataByRole(int userId, int roleId, User user)
        {
            return roleId switch
            {
                1 => await GetOfficerData(userId, user), // Officer/Buyer
                2 => await GetManagerData(userId, user),  // Manager
                3 => await GetDirectorData(userId, user), // Director
                4 => await GetCFOData(userId, user),      // CFO
                _ => await GetOfficerData(userId, user)   // Default
            };
        }

        private async Task<BuyerProfileViewModel> GetOfficerData(int userId, User user)
        {
            // Mevcut Buyer logic'i
            Budget? latestBudget = null;
            if (user.DepartmentID.HasValue)
            {
                latestBudget = await _context.Budgets
                    .AsNoTracking()
                    .Where(b => b.DepartmentID == user.DepartmentID)
                    .OrderByDescending(b => b.FiscalYear)
                    .FirstOrDefaultAsync();
            }

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = latestBudget?.TotalBudget ?? 10000m,
                CommittedSpend = latestBudget?.AmountCommitted ?? 2400m
            };

            var pendingApprovals = await _context.PurchaseRequests
                .AsNoTracking()
                .Where(pr => pr.RequesterID == userId && pr.PurchaseOrder == null && pr.PRStatus != PRStatusType.Rejected)
                .OrderByDescending(pr => pr.DateSubmitted)
                .Take(5)
                .Select(pr => new PendingApprovalViewModel
                {
                    RequestId = pr.PRID,
                    Reference = $"PR-{pr.PRID:0000}",
                    Amount = pr.EstimatedTotal,
                    Stage = pr.PRStatus.ToString()
                })
                .ToListAsync();

            var recentlyApproved = await _context.PurchaseRequests
                .AsNoTracking()
                .Where(pr => pr.RequesterID == userId && pr.PurchaseOrder != null)
                .OrderByDescending(pr => pr.DateSubmitted)
                .Take(5)
                .Select(pr => new RecentlyApprovedViewModel
                {
                    Reference = $"PR-{pr.PRID:0000}",
                    Amount = pr.EstimatedTotal,
                    FinalApprovalRole = pr.PurchaseOrder != null ? pr.PurchaseOrder.POStatus.ToString() : "--"
                })
                .ToListAsync();

            // Officer için approval ladder
            var approvalLadder = new List<ApprovalLadderStepViewModel>
            {
                new() { Role = "Officer", ThresholdPercent = 25m, Description = "You can create purchases up to 25% of department budget." },
                new() { Role = "Manager", ThresholdPercent = 50m, Description = "Manager approval required between 25% and 50%." },
                new() { Role = "Director", ThresholdPercent = 75m, Description = "Director approval required for 50% - 75% escalations." },
                new() { Role = "CFO", ThresholdPercent = 100m, Description = "CFO approves anything over 75% of budget." }
            };

            return new BuyerProfileViewModel
            {
                CompanySnapshot = companySnapshot,
                PendingApprovals = pendingApprovals,
                RecentlyApproved = recentlyApproved,
                ApprovalLadder = approvalLadder,
                Invoices = new List<InvoiceSummaryViewModel>() // Officer'lar genelde invoice takibi yapmaz
            };
        }

        private async Task<BuyerProfileViewModel> GetManagerData(int userId, User user)
        {
            // Manager için department budget
            Budget? latestBudget = null;
            if (user.DepartmentID.HasValue)
            {
                latestBudget = await _context.Budgets
                    .AsNoTracking()
                    .Where(b => b.DepartmentID == user.DepartmentID)
                    .OrderByDescending(b => b.FiscalYear)
                    .FirstOrDefaultAsync();
            }

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = latestBudget?.TotalBudget ?? 50000m,
                CommittedSpend = latestBudget?.AmountCommitted ?? 12000m
            };

            // Manager'ın onaylaması gereken PR'ları
            var pendingApprovals = await _context.ApprovalHistories
                .AsNoTracking()
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Forwarded)
                .Include(ah => ah.PurchaseRequest)
                .OrderBy(ah => ah.ActionDate)
                .Take(10)
                .Select(ah => new PendingApprovalViewModel
                {
                    RequestId = ah.PRID,
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest.EstimatedTotal,
                    Stage = $"Level {ah.ApprovalLevel} - Manager Approval"
                })
                .ToListAsync();

            // Manager'ın onayladığı PR'lar
            var recentlyApproved = await _context.ApprovalHistories
                .AsNoTracking()
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Approved)
                .Include(ah => ah.PurchaseRequest)
                .OrderByDescending(ah => ah.ActionDate)
                .Take(10)
                .Select(ah => new RecentlyApprovedViewModel
                {
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest.EstimatedTotal,
                    FinalApprovalRole = "Manager"
                })
                .ToListAsync();

            var approvalLadder = new List<ApprovalLadderStepViewModel>
            {
                new() { Role = "Officer", ThresholdPercent = 12.5m, Description = "Officers handle small purchases up to 12.5% of department budget." },
                new() { Role = "Manager", ThresholdPercent = 50m, Description = "You approve purchases between 12.5% and 50%." },
                new() { Role = "Director", ThresholdPercent = 75m, Description = "Director approval required for 50% - 75% escalations." },
                new() { Role = "CFO", ThresholdPercent = 100m, Description = "CFO approves anything over 75% of department budget." }
            };

            return new BuyerProfileViewModel
            {
                CompanySnapshot = companySnapshot,
                PendingApprovals = pendingApprovals,
                RecentlyApproved = recentlyApproved,
                ApprovalLadder = approvalLadder,
                Invoices = new List<InvoiceSummaryViewModel>() // Manager seviyesinde basit invoice takibi
            };
        }

        private async Task<BuyerProfileViewModel> GetDirectorData(int userId, User user)
        {
            // Company-wide budget
            var allBudgets = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.FiscalYear == DateTime.Now.Year)
                .ToListAsync();

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = allBudgets.Sum(b => b.TotalBudget),
                CommittedSpend = allBudgets.Sum(b => b.AmountCommitted ?? 0)
            };

            var pendingApprovals = await _context.ApprovalHistories
                .AsNoTracking()
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Forwarded)
                .Include(ah => ah.PurchaseRequest)
                .OrderBy(ah => ah.ActionDate)
                .Take(15)
                .Select(ah => new PendingApprovalViewModel
                {
                    RequestId = ah.PRID,
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest.EstimatedTotal,
                    Stage = $"Level {ah.ApprovalLevel} - Director Approval"
                })
                .ToListAsync();

            var recentlyApproved = await _context.ApprovalHistories
                .AsNoTracking()
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Approved)
                .Include(ah => ah.PurchaseRequest)
                .OrderByDescending(ah => ah.ActionDate)
                .Take(15)
                .Select(ah => new RecentlyApprovedViewModel
                {
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest.EstimatedTotal,
                    FinalApprovalRole = "Director"
                })
                .ToListAsync();

            var approvalLadder = new List<ApprovalLadderStepViewModel>
            {
                new() { Role = "Officer", ThresholdPercent = 6.25m, Description = "Officers handle routine purchases up to 6.25% of company budget." },
                new() { Role = "Manager", ThresholdPercent = 25m, Description = "Managers handle departmental approvals between 6.25% and 25%." },
                new() { Role = "Director", ThresholdPercent = 75m, Description = "You approve major purchases between 25% and 75%." },
                new() { Role = "CFO", ThresholdPercent = 100m, Description = "CFO handles strategic investments over 75% of company budget." }
            };

            return new BuyerProfileViewModel
            {
                CompanySnapshot = companySnapshot,
                PendingApprovals = pendingApprovals,
                RecentlyApproved = recentlyApproved,
                ApprovalLadder = approvalLadder,
                Invoices = new List<InvoiceSummaryViewModel>() // Director seviyesinde strategic invoice takibi
            };
        }

        private async Task<BuyerProfileViewModel> GetCFOData(int userId, User user)
        {
            // Company-wide financial overview
            var allBudgets = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.FiscalYear == DateTime.Now.Year)
                .ToListAsync();

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = allBudgets.Sum(b => b.TotalBudget),
                CommittedSpend = allBudgets.Sum(b => b.AmountCommitted ?? 0)
            };

            var pendingApprovals = await _context.ApprovalHistories
                .AsNoTracking()
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Forwarded)
                .Include(ah => ah.PurchaseRequest)
                .OrderBy(ah => ah.ActionDate)
                .Take(20)
                .Select(ah => new PendingApprovalViewModel
                {
                    RequestId = ah.PRID,
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest.EstimatedTotal,
                    Stage = $"Level {ah.ApprovalLevel} - CFO Final Approval"
                })
                .ToListAsync();

            var recentlyApproved = await _context.ApprovalHistories
                .AsNoTracking()
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Approved)
                .Include(ah => ah.PurchaseRequest)
                .OrderByDescending(ah => ah.ActionDate)
                .Take(20)
                .Select(ah => new RecentlyApprovedViewModel
                {
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest.EstimatedTotal,
                    FinalApprovalRole = "CFO"
                })
                .ToListAsync();

            var approvalLadder = new List<ApprovalLadderStepViewModel>
            {
                new() { Role = "Officer", ThresholdPercent = 3.125m, Description = "Officers handle routine operational purchases up to 3.125% of total budget." },
                new() { Role = "Manager", ThresholdPercent = 12.5m, Description = "Managers handle departmental strategic purchases between 3.125% and 12.5%." },
                new() { Role = "Director", ThresholdPercent = 37.5m, Description = "Directors handle cross-departmental initiatives between 12.5% and 37.5%." },
                new() { Role = "CFO", ThresholdPercent = 100m, Description = "You have final authority over all strategic investments above 37.5%." }
            };

            return new BuyerProfileViewModel
            {
                CompanySnapshot = companySnapshot,
                PendingApprovals = pendingApprovals,
                RecentlyApproved = recentlyApproved,
                ApprovalLadder = approvalLadder,
                Invoices = new List<InvoiceSummaryViewModel>() // CFO seviyesinde full financial oversight
            };
        }

        // Approval işlemleri - ApprovalController'a yönlendirme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            return RedirectToAction("Approve", "Approval", new { id = requestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateOrder(BuyerRateOrderInput input)
        {
            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] = "Please choose a rating before submitting.";
                return RedirectToAction(nameof(Profile));
            }

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var existingRating = await _context.SupplierRatings
                    .FirstOrDefaultAsync(r => r.POID == input.PurchaseOrderId && r.RatedByUserID == userId);

                if (existingRating == null)
                {
                    var newRating = new SupplierRating
                    {
                        POID = input.PurchaseOrderId,
                        RatedByUserID = userId,
                        RatingScore = input.RatingScore,
                        RatedOn = DateTime.UtcNow
                    };
                    _context.SupplierRatings.Add(newRating);
                }
                else
                {
                    existingRating.RatingScore = input.RatingScore;
                    existingRating.RatedOn = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                TempData["ProfileMessage"] = "Thanks for rating this order.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit rating for user {UserId}", userId);
                TempData["ProfileError"] = "Something went wrong while submitting your rating.";
            }

            return RedirectToAction(nameof(Profile));
        }

        private BuyerProfileViewModel CreateSampleProfile(int roleId)
        {
            // Role'e göre sample data oluştur
            return roleId switch
            {
                2 => CreateSampleManagerProfile(),
                3 => CreateSampleDirectorProfile(),
                4 => CreateSampleCFOProfile(),
                _ => CreateSampleOfficerProfile()
            };
        }

        // Sample profile metodları (mevcut CreateSampleProfile'ı böl)
        private BuyerProfileViewModel CreateSampleOfficerProfile()
        {
            // Mevcut sample profile logic
            var now = DateTime.UtcNow;

            var pendingApprovals = new List<PendingApprovalViewModel>
            {
                new() { RequestId = 4101, Reference = "PR-4101", Amount = 1900m, Stage = PRStatusType.PendingApproval.ToString() },
                new() { RequestId = 4203, Reference = "PR-4203", Amount = 3750m, Stage = PRStatusType.PendingApproval.ToString() }
            };

            var approvalLadder = new List<ApprovalLadderStepViewModel>
            {
                new() { Role = "Officer", ThresholdPercent = 25m, Description = "You can create purchases up to 25% of department budget." },
                new() { Role = "Manager", ThresholdPercent = 50m, Description = "Manager approval required between 25% and 50%." },
                new() { Role = "Director", ThresholdPercent = 75m, Description = "Director approval required for 50% - 75% escalations." },
                new() { Role = "CFO", ThresholdPercent = 100m, Description = "CFO approves anything over 75% of budget." }
            };

            return new BuyerProfileViewModel
            {
                CompanySnapshot = new CompanySnapshotViewModel { TotalBudget = 10000m, CommittedSpend = 2400m },
                PendingApprovals = pendingApprovals,
                ApprovalLadder = approvalLadder,
                RecentlyApproved = new List<RecentlyApprovedViewModel>(),
                Invoices = new List<InvoiceSummaryViewModel>()
            };
        }

        private BuyerProfileViewModel CreateSampleManagerProfile()
        {
            // Manager sample data
            return new BuyerProfileViewModel
            {
                CompanySnapshot = new CompanySnapshotViewModel { TotalBudget = 50000m, CommittedSpend = 12000m },
                PendingApprovals = new List<PendingApprovalViewModel>
                {
                    new() { RequestId = 5001, Reference = "PR-5001", Amount = 15000m, Stage = "Level 2 - Manager Approval" }
                },
                ApprovalLadder = new List<ApprovalLadderStepViewModel>
                {
                    new() { Role = "Manager", ThresholdPercent = 50m, Description = "You approve purchases between 12.5% and 50%." }
                },
                RecentlyApproved = new List<RecentlyApprovedViewModel>(),
                Invoices = new List<InvoiceSummaryViewModel>()
            };
        }

        private BuyerProfileViewModel CreateSampleDirectorProfile()
        {
            return new BuyerProfileViewModel
            {
                CompanySnapshot = new CompanySnapshotViewModel { TotalBudget = 500000m, CommittedSpend = 180000m },
                PendingApprovals = new List<PendingApprovalViewModel>
                {
                    new() { RequestId = 7001, Reference = "PR-7001", Amount = 95000m, Stage = "Level 3 - Director Approval" }
                },
                ApprovalLadder = new List<ApprovalLadderStepViewModel>
                {
                    new() { Role = "Director", ThresholdPercent = 75m, Description = "You approve major purchases between 25% and 75%." }
                },
                RecentlyApproved = new List<RecentlyApprovedViewModel>(),
                Invoices = new List<InvoiceSummaryViewModel>()
            };
        }

        private BuyerProfileViewModel CreateSampleCFOProfile()
        {
            return new BuyerProfileViewModel
            {
                CompanySnapshot = new CompanySnapshotViewModel { TotalBudget = 1000000m, CommittedSpend = 420000m },
                PendingApprovals = new List<PendingApprovalViewModel>
                {
                    new() { RequestId = 8001, Reference = "PR-8001", Amount = 275000m, Stage = "Level 4 - CFO Final Approval" }
                },
                ApprovalLadder = new List<ApprovalLadderStepViewModel>
                {
                    new() { Role = "CFO", ThresholdPercent = 100m, Description = "You have final authority over all strategic investments above 37.5%." }
                },
                RecentlyApproved = new List<RecentlyApprovedViewModel>(),
                Invoices = new List<InvoiceSummaryViewModel>()
            };
        }

        private static bool IsProfileConnectivityIssue(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is SqlException || current is SocketException || current is TaskCanceledException)
                {
                    return true;
                }
            }
            return false;
        }
    }
}