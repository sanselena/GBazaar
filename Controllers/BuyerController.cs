using GBazaar.Models;
using GBazaar.Models.Enums;
using GBazaar.ViewModels.Buyer;
using Gbazaar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GBazaar.Controllers
{
    public class BuyerController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly ILogger<BuyerController> _logger;

        public BuyerController(ProcurementContext context, ILogger<BuyerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Buyer/Profile
        public async Task<IActionResult> Profile()
        {
            ViewBag.UserType = "Buyer";

            const int buyerUserId = 1; // TODO: Replace with authenticated buyer id

            try
            {
                var buyer = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.UserID == buyerUserId);

                if (buyer == null)
                {
                    TempData["ProfileError"] = "We could not locate your buyer record.";
                    return View(CreateSampleProfile());
                }

                Budget? latestBudget = null;
                if (buyer.DepartmentID.HasValue)
                {
                    latestBudget = await _context.Budgets
                        .AsNoTracking()
                        .Where(b => b.DepartmentID == buyer.DepartmentID)
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
                    .Where(pr => pr.RequesterID == buyerUserId && pr.PurchaseOrder == null && pr.PRStatus != PRStatusType.Rejected)
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
                    .Where(pr => pr.RequesterID == buyerUserId && pr.PurchaseOrder != null)
                    .OrderByDescending(pr => pr.DateSubmitted)
                    .Take(5)
                    .Select(pr => new RecentlyApprovedViewModel
                    {
                        Reference = $"PR-{pr.PRID:0000}",
                        Amount = pr.EstimatedTotal,
                        FinalApprovalRole = pr.PurchaseOrder != null ? pr.PurchaseOrder.POStatus.ToString() : "--"
                    })
                    .ToListAsync();

                var buyerOrders = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Where(po => po.PurchaseRequest != null && po.PurchaseRequest.RequesterID == buyerUserId && po.POStatus != POStatusType.Rejected)
                    .Include(po => po.PurchaseRequest)
                        .ThenInclude(pr => pr.Supplier)
                    .Include(po => po.Supplier)
                    .Include(po => po.POItems)
                    .Include(po => po.Invoices)
                    .Include(po => po.SupplierRatings)
                    .OrderByDescending(po => po.DateIssued)
                    .ToListAsync();

                var invoiceSummaries = new List<InvoiceSummaryViewModel>();

                foreach (var po in buyerOrders)
                {
                    var latestInvoice = po.Invoices
                        .OrderByDescending(i => i.InvoiceDate)
                        .ThenByDescending(i => i.InvoiceID)
                        .FirstOrDefault();

                    if (latestInvoice == null)
                    {
                        continue;
                    }

                    var paymentStatus = latestInvoice.PaymentStatus;
                    var totalAmount = po.POItems.Any()
                        ? po.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice)
                        : latestInvoice.AmountDue ?? 0m;
                    var isDelivered = po.POStatus == POStatusType.FullyReceived || po.POStatus == POStatusType.Closed;
                    var isPaymentComplete = paymentStatus == PaymentStatusType.Paid;

                    if (isDelivered && isPaymentComplete)
                    {
                        continue;
                    }

                    var reference = po.PurchaseRequest != null
                        ? $"PR-{po.PurchaseRequest.PRID:0000}"
                        : $"PO-{po.POID:0000}";

                    var existingRating = po.SupplierRatings
                        .FirstOrDefault(r => r.RatedByUserID == buyerUserId)?.RatingScore;

                    var invoiceItems = po.POItems
                        .OrderBy(item => item.POItemID)
                        .Select(item => new InvoiceLineSummaryViewModel
                        {
                            ItemName = item.ItemName,
                            Quantity = item.QuantityOrdered,
                            UnitPrice = item.UnitPrice
                        })
                        .ToList();

                    invoiceSummaries.Add(new InvoiceSummaryViewModel
                    {
                        InvoiceId = latestInvoice.InvoiceID,
                        PurchaseOrderId = po.POID,
                        Reference = reference,
                        InvoiceNumber = latestInvoice.InvoiceNumber,
                        SupplierName = po.Supplier?.SupplierName ?? "Unknown Supplier",
                        AmountDue = latestInvoice.AmountDue,
                        TotalAmount = totalAmount,
                        OutstandingAmount = latestInvoice.AmountDue ?? totalAmount,
                        InvoiceDate = latestInvoice.InvoiceDate,
                        DueDate = latestInvoice.DueDate,
                        ExpectedDelivery = po.RequiredDeliveryDate,
                        PaymentStatus = paymentStatus,
                        FulfillmentStatus = po.POStatus,
                        ExistingRating = existingRating,
                        Items = invoiceItems
                    });
                }

                var approvalLadder = new List<ApprovalLadderStepViewModel>
                {
                    new() { Role = "PO", ThresholdPercent = 25m, Description = "Handles purchases up to 25% of total budget." },
                    new() { Role = "Manager", ThresholdPercent = 50m, Description = "Steps in between 25% and 50%." },
                    new() { Role = "Director", ThresholdPercent = 75m, Description = "Required for 50% - 75% escalations." },
                    new() { Role = "CFO", ThresholdPercent = 100m, Description = "Approves anything over 75% and manages budgets." }
                };

                var model = new BuyerProfileViewModel
                {
                    CompanySnapshot = companySnapshot,
                    PendingApprovals = pendingApprovals,
                    RecentlyApproved = recentlyApproved,
                    ApprovalLadder = approvalLadder,
                    Invoices = invoiceSummaries
                };

                return View(model);
            }
            catch (Exception ex) when (IsProfileConnectivityIssue(ex))
            {
                _logger.LogWarning(ex, "Falling back to buyer profile sample data due to connectivity issues.");
                TempData["ProfileError"] = "We couldn't reach the procurement database, showing sample data instead.";
                return View(CreateSampleProfile());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load buyer profile.");
                TempData["ProfileError"] = "Something went wrong while loading your profile. Showing sample data instead.";
                return View(CreateSampleProfile());
            }
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

            const int buyerUserId = 1;
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.PurchaseOrder)
                        .ThenInclude(po => po.PurchaseRequest)
                    .FirstOrDefaultAsync(i => i.InvoiceID == input.InvoiceId && i.POID == input.PurchaseOrderId);

                if (invoice?.PurchaseOrder?.PurchaseRequest?.RequesterID != buyerUserId)
                {
                    TempData["ProfileError"] = "Invoice not found for your account.";
                    return RedirectToAction(nameof(Profile));
                }

                var existingRating = await _context.SupplierRatings
                    .FirstOrDefaultAsync(r => r.POID == input.PurchaseOrderId && r.RatedByUserID == buyerUserId);

                if (existingRating == null)
                {
                    var newRating = new SupplierRating
                    {
                        POID = input.PurchaseOrderId,
                        RatedByUserID = buyerUserId,
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
            catch (Exception ex) when (IsProfileConnectivityIssue(ex))
            {
                _logger.LogWarning(ex, "Unable to submit rating due to connectivity issues.");
                TempData["ProfileError"] = "We couldn't submit your rating because the procurement database is offline.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit rating.");
                TempData["ProfileError"] = "Something went wrong while submitting your rating.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            const int buyerUserId = 1;

            try
            {
                var request = await _context.PurchaseRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pr => pr.PRID == requestId && pr.RequesterID == buyerUserId);

                if (request == null)
                {
                    TempData["ProfileError"] = "Request not found or already processed.";
                    return RedirectToAction(nameof(Profile));
                }

                _logger.LogInformation("Buyer {BuyerId} queued approval for PR-{RequestId}", buyerUserId, requestId);
                TempData["ProfileMessage"] = $"Request PR-{requestId:0000} is queued for approval. Workflow wiring is coming soon.";
            }
            catch (Exception ex) when (IsProfileConnectivityIssue(ex))
            {
                _logger.LogWarning(ex, "Unable to queue approval due to connectivity issues.");
                TempData["ProfileError"] = "We couldn't submit your approval because the procurement database is offline.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue approval.");
                TempData["ProfileError"] = "Something went wrong while submitting your approval.";
            }

            return RedirectToAction(nameof(Profile));
        }

        private BuyerProfileViewModel CreateSampleProfile()
        {
            var now = DateTime.UtcNow;

            var pendingApprovals = new List<PendingApprovalViewModel>
            {
                new()
                {
                    RequestId = 4101,
                    Reference = "PR-4101",
                    Amount = 1900m,
                    Stage = PRStatusType.PendingApproval.ToString()
                },
                new()
                {
                    RequestId = 4203,
                    Reference = "PR-4203",
                    Amount = 3750m,
                    Stage = PRStatusType.PendingApproval.ToString()
                }
            };

            var approvalLadder = new List<ApprovalLadderStepViewModel>
            {
                new() { Role = "PO", ThresholdPercent = 25m, Description = "Handles purchases up to 25% of total budget." },
                new() { Role = "Manager", ThresholdPercent = 50m, Description = "Steps in between 25% and 50%." },
                new() { Role = "Director", ThresholdPercent = 75m, Description = "Required for 50% - 75% escalations." },
                new() { Role = "CFO", ThresholdPercent = 100m, Description = "Approves anything over 75% and manages budgets." }
            };

            var recentlyApproved = new List<RecentlyApprovedViewModel>
            {
                new() { Reference = "PO-1002", Amount = 2150m, FinalApprovalRole = "PO" },
                new() { Reference = "PR-2021", Amount = 4800m, FinalApprovalRole = "Manager" },
                new() { Reference = "PR-2034", Amount = 7900m, FinalApprovalRole = "Director" }
            };

            var invoices = new List<InvoiceSummaryViewModel>
            {
                new()
                {
                    InvoiceId = 8891,
                    PurchaseOrderId = 5051,
                    Reference = "PR-5051",
                    InvoiceNumber = "INV-5051",
                    SupplierName = "Northwind Purchasing",
                    AmountDue = 7825m,
                    TotalAmount = 7825m,
                    OutstandingAmount = 7825m,
                    InvoiceDate = DateOnly.FromDateTime(now.AddDays(-3)),
                    DueDate = DateOnly.FromDateTime(now.AddDays(12)),
                    ExpectedDelivery = DateOnly.FromDateTime(now.AddDays(5)),
                    PaymentStatus = PaymentStatusType.PartiallyPaid,
                    FulfillmentStatus = POStatusType.PartiallyReceived,
                    ExistingRating = null,
                    Items = new List<InvoiceLineSummaryViewModel>
                    {
                        new()
                        {
                            ItemName = "Industrial Fasteners",
                            Quantity = 200,
                            UnitPrice = 8.50m
                        },
                        new()
                        {
                            ItemName = "Precision Bearings",
                            Quantity = 50,
                            UnitPrice = 52m
                        }
                    }
                },
                new()
                {
                    InvoiceId = 8920,
                    PurchaseOrderId = 5052,
                    Reference = "PR-5052",
                    InvoiceNumber = "INV-5052",
                    SupplierName = "Contoso Manufacturing",
                    AmountDue = 3120m,
                    TotalAmount = 3120m,
                    OutstandingAmount = 3120m,
                    InvoiceDate = DateOnly.FromDateTime(now.AddDays(-5)),
                    DueDate = DateOnly.FromDateTime(now.AddDays(9)),
                    ExpectedDelivery = DateOnly.FromDateTime(now.AddDays(2)),
                    PaymentStatus = PaymentStatusType.NotPaid,
                    FulfillmentStatus = POStatusType.Issued,
                    ExistingRating = 4,
                    Items = new List<InvoiceLineSummaryViewModel>
                    {
                        new()
                        {
                            ItemName = "Cooling Fans",
                            Quantity = 25,
                            UnitPrice = 45m
                        }
                    }
                }
            };

            return new BuyerProfileViewModel
            {
                CompanySnapshot = new CompanySnapshotViewModel
                {
                    TotalBudget = 10000m,
                    CommittedSpend = 2400m
                },
                PendingApprovals = pendingApprovals,
                ApprovalLadder = approvalLadder,
                RecentlyApproved = recentlyApproved,
                Invoices = invoices
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
