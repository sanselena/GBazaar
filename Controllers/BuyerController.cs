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
                    .Include(u => u.Department)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    TempData["ProfileError"] = "User record not found.";
                    return View(new BuyerProfileViewModel());
                }

                // Role'e göre ViewBag ayarla
                SetViewBagByRole(userRoleId, user);

                // Role'e göre veri getir
                var model = await GetProfileDataByRole(userId, userRoleId, user);

                return View(model);
            }
            catch (Exception ex) when (IsProfileConnectivityIssue(ex))
            {
                _logger.LogWarning(ex, "Database connectivity issue occurred.");
                TempData["ProfileError"] = "We couldn't reach the procurement database. Please try again later.";
                return View(new BuyerProfileViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profile for user {UserId} with role {RoleId}", userId, userRoleId);
                TempData["ProfileError"] = "Something went wrong while loading your profile. Please try again.";
                return View(new BuyerProfileViewModel());
            }
        }

        private void SetViewBagByRole(int roleId, User user)
        {
            switch (roleId)
            {
                case 1: // Officer
                    ViewBag.UserType = "Buyer";
                    ViewBag.UserName = user.FullName ?? "Unknown User";
                    ViewBag.DepartmentName = user.Department?.DepartmentName ?? "Unknown Department";
                    break;
                case 2: // Manager
                    ViewBag.UserType = "Manager";
                    ViewBag.UserName = user.FullName ?? "Unknown User";
                    ViewBag.DepartmentName = user.Department?.DepartmentName ?? "Unknown Department";
                    break;
                case 3: // Director
                    ViewBag.UserType = "Director";
                    ViewBag.UserName = user.FullName ?? "Unknown User";
                    ViewBag.DepartmentName = user.Department?.DepartmentName ?? "Executive";
                    break;
                case 4: // CFO
                    ViewBag.UserType = "CFO";
                    ViewBag.UserName = user.FullName ?? "Unknown User";
                    ViewBag.DepartmentName = "Finance & Operations";
                    break;
                default:
                    ViewBag.UserType = "Buyer";
                    ViewBag.UserName = user.FullName ?? "Unknown User";
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
            // Budget data
            Budget? latestBudget = null;
            if (user.DepartmentID.HasValue)
            {
                latestBudget = await _context.Budgets
                    .Where(b => b.DepartmentID == user.DepartmentID)
                    .OrderByDescending(b => b.FiscalYear)
                    .FirstOrDefaultAsync();
            }

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = latestBudget?.TotalBudget ?? 0m,
                CommittedSpend = latestBudget?.AmountCommitted ?? 0m
            };

            // Pending approvals - Purchase requests by this user that don't have a PO yet
            var pendingApprovals = await _context.PurchaseRequests
                .Where(pr => pr.RequesterID == userId &&
                            pr.PurchaseOrder == null &&
                            pr.PRStatus != PRStatusType.Rejected)
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

            // Recently approved - Purchase requests by this user that have been converted to POs
            var recentlyApproved = await _context.PurchaseRequests
                .Where(pr => pr.RequesterID == userId && pr.PurchaseOrder != null)
                .Include(pr => pr.PurchaseOrder!)
                    .ThenInclude(po => po.Invoices)
                .OrderByDescending(pr => pr.DateSubmitted)
                .Take(5)
                .Select(pr => new GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel
                {
                    Reference = $"PR-{pr.PRID:0000}",
                    Amount = pr.EstimatedTotal,
                    FinalApprovalRole = pr.PurchaseOrder!.POStatus.ToString(),
                    PurchaseOrderId = pr.PurchaseOrder.POID,
                    POStatus = pr.PurchaseOrder.POStatus,
                    HasInvoice = pr.PurchaseOrder.Invoices.Any(),
                    CanMakePayment = pr.PurchaseOrder.POStatus == POStatusType.Issued &&
                                   !pr.PurchaseOrder.Invoices.Any(),
                    PaymentStatus = pr.PurchaseOrder.Invoices.Any()
                                  ? pr.PurchaseOrder.Invoices.First().PaymentStatus
                                  : null
                })
                .ToListAsync();

            // In-Flight Invoices - Bu kullanıcının oluşturduğu aktif invoice'lar
            var invoices = await _context.Invoices
                .Where(i => i.PurchaseOrder!.PurchaseRequest!.RequesterID == userId &&
                           i.PaymentStatus != PaymentStatusType.Paid)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.POItems)
                .Include(i => i.Supplier)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.SupplierRatings)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(10)
                .Select(i => new InvoiceSummaryViewModel
                {
                    InvoiceId = i.InvoiceID,
                    PurchaseOrderId = i.POID,
                    Reference = $"PO-{i.POID:0000}",
                    InvoiceNumber = i.InvoiceNumber,
                    SupplierName = i.Supplier!.SupplierName,
                    AmountDue = i.AmountDue,
                    TotalAmount = i.PurchaseOrder!.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice),
                    OutstandingAmount = i.AmountDue ?? 0,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    ExpectedDelivery = i.PurchaseOrder.RequiredDeliveryDate,
                    PaymentStatus = i.PaymentStatus,
                    FulfillmentStatus = i.PurchaseOrder.POStatus,
                    ExistingRating = i.PurchaseOrder.SupplierRatings
                                   .Where(sr => sr.RatedByUserID == userId)
                                   .Select(sr => sr.RatingScore)
                                   .FirstOrDefault(),
                    Items = i.PurchaseOrder.POItems.Select(item => new InvoiceLineSummaryViewModel
                    {
                        ItemName = item.ItemName,
                        Quantity = item.QuantityOrdered,
                        UnitPrice = item.UnitPrice
                    }).ToList()
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
                RecentlyApproved = recentlyApproved.Cast<GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel>().ToList(),
                ApprovalLadder = approvalLadder,
                Invoices = invoices
            };
        }

        private async Task<BuyerProfileViewModel> GetManagerData(int userId, User user)
        {
            Budget? latestBudget = null;
            if (user.DepartmentID.HasValue)
            {
                latestBudget = await _context.Budgets
                    .Where(b => b.DepartmentID == user.DepartmentID)
                    .OrderByDescending(b => b.FiscalYear)
                    .FirstOrDefaultAsync();
            }

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = latestBudget?.TotalBudget ?? 0m,
                CommittedSpend = latestBudget?.AmountCommitted ?? 0m
            };

            // For managers, show requests that are pending their approval
            var pendingApprovals = await _context.ApprovalHistories
                .Where(ah => ah.ApproverID == userId &&
                           ah.ActionType == ApprovalActionType.Forwarded)
                .Include(ah => ah.PurchaseRequest)
                .OrderBy(ah => ah.ActionDate)
                .Take(10)
                .Select(ah => new PendingApprovalViewModel
                {
                    RequestId = ah.PRID,
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest!.EstimatedTotal,
                    Stage = $"Level {ah.ApprovalLevel} - Manager Approval"
                })
                .ToListAsync();

            // Show requests this manager has approved
            var recentlyApproved = await _context.ApprovalHistories
                .Where(ah => ah.ApproverID == userId &&
                           ah.ActionType == ApprovalActionType.Approved)
                .Include(ah => ah.PurchaseRequest!)
                    .ThenInclude(pr => pr.PurchaseOrder!)
                        .ThenInclude(po => po.Invoices)
                .OrderByDescending(ah => ah.ActionDate)
                .Take(10)
                .Select(ah => new GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel
                {
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest!.EstimatedTotal,
                    FinalApprovalRole = "Manager",
                    PurchaseOrderId = ah.PurchaseRequest.PurchaseOrder != null ? ah.PurchaseRequest.PurchaseOrder.POID : 0,
                    POStatus = ah.PurchaseRequest.PurchaseOrder != null ? ah.PurchaseRequest.PurchaseOrder.POStatus : POStatusType.PendingSupplierApproval,
                    HasInvoice = ah.PurchaseRequest.PurchaseOrder != null && ah.PurchaseRequest.PurchaseOrder.Invoices.Any(),
                    CanMakePayment = ah.PurchaseRequest.PurchaseOrder != null &&
                                   ah.PurchaseRequest.PurchaseOrder.POStatus == POStatusType.Issued &&
                                   !ah.PurchaseRequest.PurchaseOrder.Invoices.Any(),
                    PaymentStatus = ah.PurchaseRequest.PurchaseOrder != null && ah.PurchaseRequest.PurchaseOrder.Invoices.Any()
                                  ? ah.PurchaseRequest.PurchaseOrder.Invoices.First().PaymentStatus
                                  : null
                })
                .ToListAsync();

            // Manager'ın onayladığı PO'lardan oluşan invoices
            var invoices = await _context.Invoices
                .Where(i => _context.ApprovalHistories
                    .Any(ah => ah.ApproverID == userId &&
                              ah.ActionType == ApprovalActionType.Approved &&
                              ah.PRID == i.PurchaseOrder!.PRID) &&
                           i.PaymentStatus != PaymentStatusType.Paid)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.POItems)
                .Include(i => i.Supplier)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.SupplierRatings)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(10)
                .Select(i => new InvoiceSummaryViewModel
                {
                    InvoiceId = i.InvoiceID,
                    PurchaseOrderId = i.POID,
                    Reference = $"PO-{i.POID:0000}",
                    InvoiceNumber = i.InvoiceNumber,
                    SupplierName = i.Supplier!.SupplierName,
                    AmountDue = i.AmountDue,
                    TotalAmount = i.PurchaseOrder!.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice),
                    OutstandingAmount = i.AmountDue ?? 0,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    ExpectedDelivery = i.PurchaseOrder.RequiredDeliveryDate,
                    PaymentStatus = i.PaymentStatus,
                    FulfillmentStatus = i.PurchaseOrder.POStatus,
                    ExistingRating = i.PurchaseOrder.SupplierRatings
                                   .Where(sr => sr.RatedByUserID == userId)
                                   .Select(sr => sr.RatingScore)
                                   .FirstOrDefault(),
                    Items = i.PurchaseOrder.POItems.Select(item => new InvoiceLineSummaryViewModel
                    {
                        ItemName = item.ItemName,
                        Quantity = item.QuantityOrdered,
                        UnitPrice = item.UnitPrice
                    }).ToList()
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
                RecentlyApproved = recentlyApproved.Cast<GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel>().ToList(),
                ApprovalLadder = approvalLadder,
                Invoices = invoices
            };
        }

        private async Task<BuyerProfileViewModel> GetDirectorData(int userId, User user)
        {
            var allBudgets = await _context.Budgets
                .Where(b => b.FiscalYear == DateTime.Now.Year)
                .ToListAsync();

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = allBudgets.Sum(b => b.TotalBudget),
                CommittedSpend = allBudgets.Sum(b => b.AmountCommitted ?? 0)
            };

            var pendingApprovals = await _context.ApprovalHistories
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Forwarded)
                .Include(ah => ah.PurchaseRequest)
                .OrderBy(ah => ah.ActionDate)
                .Take(15)
                .Select(ah => new PendingApprovalViewModel
                {
                    RequestId = ah.PRID,
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest!.EstimatedTotal,
                    Stage = $"Level {ah.ApprovalLevel} - Director Approval"
                })
                .ToListAsync();

            var recentlyApproved = await _context.ApprovalHistories
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Approved)
                .Include(ah => ah.PurchaseRequest!)
                    .ThenInclude(pr => pr.PurchaseOrder!)
                        .ThenInclude(po => po.Invoices)
                .OrderByDescending(ah => ah.ActionDate)
                .Take(10)
                .Select(ah => new GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel
                {
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest!.EstimatedTotal,
                    FinalApprovalRole = "Director",
                    PurchaseOrderId = ah.PurchaseRequest.PurchaseOrder != null ? ah.PurchaseRequest.PurchaseOrder.POID : 0,
                    POStatus = ah.PurchaseRequest.PurchaseOrder != null ? ah.PurchaseRequest.PurchaseOrder.POStatus : POStatusType.PendingSupplierApproval,
                    HasInvoice = ah.PurchaseRequest.PurchaseOrder != null && ah.PurchaseRequest.PurchaseOrder.Invoices.Any(),
                    CanMakePayment = ah.PurchaseRequest.PurchaseOrder != null &&
                                   ah.PurchaseRequest.PurchaseOrder.POStatus == POStatusType.Issued &&
                                   !ah.PurchaseRequest.PurchaseOrder.Invoices.Any(),
                    PaymentStatus = ah.PurchaseRequest.PurchaseOrder != null && ah.PurchaseRequest.PurchaseOrder.Invoices.Any()
                                  ? ah.PurchaseRequest.PurchaseOrder.Invoices.First().PaymentStatus
                                  : null
                })
                .ToListAsync();

            // Director'ın onayladığı PO'lardan oluşan invoices
            var invoices = await _context.Invoices
                .Where(i => _context.ApprovalHistories
                    .Any(ah => ah.ApproverID == userId &&
                              ah.ActionType == ApprovalActionType.Approved &&
                              ah.PRID == i.PurchaseOrder!.PRID) &&
                           i.PaymentStatus != PaymentStatusType.Paid)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.POItems)
                .Include(i => i.Supplier)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.SupplierRatings)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(15)
                .Select(i => new InvoiceSummaryViewModel
                {
                    InvoiceId = i.InvoiceID,
                    PurchaseOrderId = i.POID,
                    Reference = $"PO-{i.POID:0000}",
                    InvoiceNumber = i.InvoiceNumber,
                    SupplierName = i.Supplier!.SupplierName,
                    AmountDue = i.AmountDue,
                    TotalAmount = i.PurchaseOrder!.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice),
                    OutstandingAmount = i.AmountDue ?? 0,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    ExpectedDelivery = i.PurchaseOrder.RequiredDeliveryDate,
                    PaymentStatus = i.PaymentStatus,
                    FulfillmentStatus = i.PurchaseOrder.POStatus,
                    ExistingRating = i.PurchaseOrder.SupplierRatings
                                   .Where(sr => sr.RatedByUserID == userId)
                                   .Select(sr => sr.RatingScore)
                                   .FirstOrDefault(),
                    Items = i.PurchaseOrder.POItems.Select(item => new InvoiceLineSummaryViewModel
                    {
                        ItemName = item.ItemName,
                        Quantity = item.QuantityOrdered,
                        UnitPrice = item.UnitPrice
                    }).ToList()
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
                RecentlyApproved = recentlyApproved.Cast<GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel>().ToList(),
                ApprovalLadder = approvalLadder,
                Invoices = invoices
            };
        }

        private async Task<BuyerProfileViewModel> GetCFOData(int userId, User user)
        {
            var allBudgets = await _context.Budgets
                .Where(b => b.FiscalYear == DateTime.Now.Year)
                .ToListAsync();

            var companySnapshot = new CompanySnapshotViewModel
            {
                TotalBudget = allBudgets.Sum(b => b.TotalBudget),
                CommittedSpend = allBudgets.Sum(b => b.AmountCommitted ?? 0)
            };

            var pendingApprovals = await _context.ApprovalHistories
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Forwarded)
                .Include(ah => ah.PurchaseRequest)
                .OrderBy(ah => ah.ActionDate)
                .Take(20)
                .Select(ah => new PendingApprovalViewModel
                {
                    RequestId = ah.PRID,
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest!.EstimatedTotal,
                    Stage = $"Level {ah.ApprovalLevel} - CFO Final Approval"
                })
                .ToListAsync();

            var recentlyApproved = await _context.ApprovalHistories
                .Where(ah => ah.ApproverID == userId && ah.ActionType == ApprovalActionType.Approved)
                .Include(ah => ah.PurchaseRequest!)
                    .ThenInclude(pr => pr.PurchaseOrder!)
                        .ThenInclude(po => po.Invoices)
                .OrderByDescending(ah => ah.ActionDate)
                .Take(10)
                .Select(ah => new GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel
                {
                    Reference = $"PR-{ah.PRID:0000}",
                    Amount = ah.PurchaseRequest!.EstimatedTotal,
                    FinalApprovalRole = "CFO",
                    PurchaseOrderId = ah.PurchaseRequest.PurchaseOrder != null ? ah.PurchaseRequest.PurchaseOrder.POID : 0,
                    POStatus = ah.PurchaseRequest.PurchaseOrder != null ? ah.PurchaseRequest.PurchaseOrder.POStatus : POStatusType.PendingSupplierApproval,
                    HasInvoice = ah.PurchaseRequest.PurchaseOrder != null && ah.PurchaseRequest.PurchaseOrder.Invoices.Any(),
                    CanMakePayment = ah.PurchaseRequest.PurchaseOrder != null &&
                                   ah.PurchaseRequest.PurchaseOrder.POStatus == POStatusType.Issued &&
                                   !ah.PurchaseRequest.PurchaseOrder.Invoices.Any(),
                    PaymentStatus = ah.PurchaseRequest.PurchaseOrder != null && ah.PurchaseRequest.PurchaseOrder.Invoices.Any()
                                  ? ah.PurchaseRequest.PurchaseOrder.Invoices.First().PaymentStatus
                                  : null
                })
                .ToListAsync();

            // CFO'nun onayladığı PO'lardan oluşan invoices
            var invoices = await _context.Invoices
                .Where(i => _context.ApprovalHistories
                    .Any(ah => ah.ApproverID == userId &&
                              ah.ActionType == ApprovalActionType.Approved &&
                              ah.PRID == i.PurchaseOrder!.PRID) &&
                           i.PaymentStatus != PaymentStatusType.Paid)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.POItems)
                .Include(i => i.Supplier)
                .Include(i => i.PurchaseOrder!)
                    .ThenInclude(po => po.SupplierRatings)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(20)
                .Select(i => new InvoiceSummaryViewModel
                {
                    InvoiceId = i.InvoiceID,
                    PurchaseOrderId = i.POID,
                    Reference = $"PO-{i.POID:0000}",
                    InvoiceNumber = i.InvoiceNumber,
                    SupplierName = i.Supplier!.SupplierName,
                    AmountDue = i.AmountDue,
                    TotalAmount = i.PurchaseOrder!.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice),
                    OutstandingAmount = i.AmountDue ?? 0,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    ExpectedDelivery = i.PurchaseOrder.RequiredDeliveryDate,
                    PaymentStatus = i.PaymentStatus,
                    FulfillmentStatus = i.PurchaseOrder.POStatus,
                    ExistingRating = i.PurchaseOrder.SupplierRatings
                                   .Where(sr => sr.RatedByUserID == userId)
                                   .Select(sr => sr.RatingScore)
                                   .FirstOrDefault(),
                    Items = i.PurchaseOrder.POItems.Select(item => new InvoiceLineSummaryViewModel
                    {
                        ItemName = item.ItemName,
                        Quantity = item.QuantityOrdered,
                        UnitPrice = item.UnitPrice
                    }).ToList()
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
                RecentlyApproved = recentlyApproved.Cast<GBazaar.ViewModels.Buyer.RecentlyApprovedViewModel>().ToList(),
                ApprovalLadder = approvalLadder,
                Invoices = invoices
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePayment(int purchaseOrderId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.PurchaseRequest)
                        .ThenInclude(pr => pr.Requester)
                            .ThenInclude(u => u.Department)
                    .Include(po => po.POItems)
                    .Include(po => po.Supplier)
                    .Include(po => po.Invoices)
                    .FirstOrDefaultAsync(po => po.POID == purchaseOrderId &&
                                             po.PurchaseRequest != null &&
                                             po.PurchaseRequest.RequesterID == userId &&
                                             (po.POStatus == POStatusType.Issued || po.POStatus == POStatusType.PartiallyReceived));

                if (purchaseOrder?.PurchaseRequest?.Requester == null)
                {
                    TempData["ProfileError"] = "Purchase order not found or not eligible for payment.";
                    return RedirectToAction(nameof(Profile));
                }

                var existingInvoice = purchaseOrder.Invoices.FirstOrDefault();
                if (existingInvoice != null)
                {
                    TempData["ProfileError"] = "Invoice already exists for this order.";
                    return RedirectToAction(nameof(Profile));
                }

                var totalAmount = purchaseOrder.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice);

                // Budget'tan düşme işlemi
                var departmentId = purchaseOrder.PurchaseRequest.Requester.DepartmentID;
                if (departmentId.HasValue)
                {
                    var latestBudget = await _context.Budgets
                        .Where(b => b.DepartmentID == departmentId.Value)
                        .OrderByDescending(b => b.FiscalYear)
                        .FirstOrDefaultAsync();

                    if (latestBudget != null)
                    {
                        // Budget'taki AmountCommitted değerini güncelle
                        latestBudget.AmountCommitted = (latestBudget.AmountCommitted ?? 0) + totalAmount;
                        _context.Budgets.Update(latestBudget);

                        _logger.LogInformation("Budget updated: Department {DepartmentId}, AmountCommitted increased by {Amount} to {NewTotal}",
                            departmentId.Value, totalAmount, latestBudget.AmountCommitted);
                    }
                }

                var invoiceNumber = $"INV-{purchaseOrder.POID:0000}-{DateTime.UtcNow:yyyyMMdd}";

                var invoice = new Invoice
                {
                    POID = purchaseOrder.POID,
                    SupplierID = purchaseOrder.SupplierID,
                    InvoiceNumber = invoiceNumber,
                    InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    AmountDue = totalAmount,
                    PaymentStatus = PaymentStatusType.NotPaid,
                    PaymentStatusID = (int)PaymentStatusType.NotPaid
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invoice {InvoiceNumber} created for PO-{POID} by user {UserId}, Budget updated",
                    invoiceNumber, purchaseOrder.POID, userId);

                TempData["ProfileMessage"] = $"Invoice {invoiceNumber} has been generated for order PO-{purchaseOrder.POID:0000}. Amount due: {totalAmount:C}. Budget updated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate invoice for PO {POID} by user {UserId}", purchaseOrderId, userId);
                TempData["ProfileError"] = "An error occurred while generating the invoice. Please try again.";
            }

            return RedirectToAction(nameof(Profile));
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