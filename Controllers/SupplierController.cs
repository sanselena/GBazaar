using GBazaar.Models.Enums;
using GBazaar.ViewModels.Supplier;
using Gbazaar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GBazaar.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(ProcurementContext context, ILogger<SupplierController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.UserType = "Supplier";

            try
            {
                const int supplierId = 1; // TODO: Replace with authenticated supplier id

                var incomingRequests = await _context.PurchaseRequests
                    .AsNoTracking()
                    .Where(pr => pr.SupplierID == supplierId && pr.PurchaseOrder == null && pr.PRStatus != PRStatusType.Rejected)
                    .Include(pr => pr.Requester)
                    .OrderByDescending(pr => pr.DateSubmitted)
                    .ToListAsync();

                var groupedRequests = incomingRequests
                    .GroupBy(pr => pr.Requester)
                    .Select(group => new IncomingRequestGroupViewModel
                    {
                        BuyerId = group.Key?.UserID ?? 0,
                        BuyerName = group.Key?.FullName ?? "Unknown Buyer",
                        Requests = group
                            .Select(pr => new IncomingRequestItemViewModel
                            {
                                RequestId = pr.PRID,
                                Reference = $"PR-{pr.PRID:0000}",
                                EstimatedTotal = pr.EstimatedTotal,
                                DateSubmitted = pr.DateSubmitted,
                                Status = pr.PRStatus,
                                NeededBy = DateOnly.FromDateTime(pr.DateSubmitted.AddDays(7))
                            })
                            .ToList()
                    })
                    .ToList();

                var supplierOrders = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Where(po => po.SupplierID == supplierId && po.POStatus != POStatusType.Rejected)
                    .Include(po => po.PurchaseRequest)
                        .ThenInclude(pr => pr.Requester)
                    .Include(po => po.Invoices)
                    .Include(po => po.POItems)
                    .OrderByDescending(po => po.DateIssued)
                    .ToListAsync();

                var activeOrders = new List<ActiveOrderViewModel>();
                var completedOrders = new List<AcceptedHistoryItemViewModel>();

                foreach (var po in supplierOrders)
                {
                    var latestInvoice = po.Invoices
                        .OrderByDescending(i => i.InvoiceDate)
                        .ThenByDescending(i => i.InvoiceID)
                        .FirstOrDefault();

                    var paymentStatus = latestInvoice?.PaymentStatus;
                    var totalAmount = po.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice);
                    var isDelivered = po.POStatus == POStatusType.FullyReceived || po.POStatus == POStatusType.Closed;
                    var isPaymentComplete = paymentStatus == PaymentStatusType.Paid;

                    if (isDelivered && isPaymentComplete)
                    {
                        completedOrders.Add(new AcceptedHistoryItemViewModel
                        {
                            Reference = po.PurchaseRequest != null ? $"PR-{po.PurchaseRequest.PRID:0000}" : $"PO-{po.POID:0000}",
                            Amount = totalAmount,
                            AcceptedOn = DateOnly.FromDateTime(po.DateIssued),
                            BuyerName = po.PurchaseRequest?.Requester?.FullName ?? "Unknown Buyer",
                            PaymentStatus = paymentStatus,
                            FulfillmentStatus = po.POStatus,
                            DeliveryDate = po.RequiredDeliveryDate,
                            InvoiceNumber = latestInvoice?.InvoiceNumber ?? string.Empty,
                            InvoiceDate = latestInvoice?.InvoiceDate,
                            PaymentDueDate = latestInvoice?.DueDate,
                            PaymentDate = latestInvoice?.PaymentDate,
                            InvoiceAmount = latestInvoice?.AmountDue ?? totalAmount
                        });
                        continue;
                    }

                    activeOrders.Add(new ActiveOrderViewModel
                    {
                        PurchaseOrderId = po.POID,
                        Reference = po.PurchaseRequest != null ? $"PR-{po.PurchaseRequest.PRID:0000}" : $"PO-{po.POID:0000}",
                        EstimatedDeliveryDate = po.RequiredDeliveryDate,
                        PaymentStatus = paymentStatus,
                        FulfillmentStatus = po.POStatus,
                        TotalAmount = totalAmount,
                        IsDelivered = isDelivered,
                        IsPaymentComplete = isPaymentComplete
                    });
                }

                var acceptedHistory = completedOrders
                    .OrderByDescending(item => item.AcceptedOn)
                    .Take(10)
                    .ToList();

                var revenueMix = completedOrders
                    .GroupBy(item => item.BuyerName)
                    .Select(group => new RevenueSliceViewModel
                    {
                        Label = group.Key,
                        Amount = group.Sum(item => item.Amount)
                    })
                    .OrderByDescending(slice => slice.Amount)
                    .ToList();

                var viewModel = new SupplierDashboardViewModel
                {
                    IncomingRequests = groupedRequests,
                    ActiveOrders = activeOrders,
                    AcceptedHistory = acceptedHistory,
                    RevenueMix = revenueMix,
                    Performance = SupplierPerformanceViewModel.Placeholder()
                };

                return View(viewModel);
            }
            catch (Exception ex) when (IsDashboardConnectivityIssue(ex))
            {
                _logger.LogWarning(ex, "Falling back to supplier dashboard sample data due to connectivity issues.");
                TempData["DashboardError"] = "We couldn't reach the procurement database, showing sample data instead.";
                return View(CreateSampleDashboard());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load supplier dashboard.");
                TempData["DashboardError"] = "Something went wrong while loading the dashboard.";
                return View(CreateSampleDashboard());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideRequest(int requestId, string decision)
        {
            const int supplierId = 1;
            var request = await _context.PurchaseRequests
                .FirstOrDefaultAsync(pr => pr.PRID == requestId && pr.SupplierID == supplierId);

            if (request == null)
            {
                TempData["DashboardError"] = "Request not found or already processed.";
                return RedirectToAction(nameof(Dashboard));
            }

            var normalizedDecision = (decision ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedDecision != "accept" && normalizedDecision != "reject")
            {
                TempData["DashboardError"] = "Unknown action.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Backend orchestration will replace this placeholder.
            _logger.LogInformation("Supplier {SupplierId} requested {Decision} for PR-{RequestId}", supplierId, normalizedDecision, requestId);
            TempData["DashboardMessage"] = $"Request PR-{requestId:0000} queued for {normalizedDecision}.";

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkDecideRequests(SupplierBulkDecisionInput input)
        {
            if (input.SelectedRequestIds.Length == 0)
            {
                TempData["DashboardError"] = "Please select at least one request before submitting a bulk decision.";
                return RedirectToAction(nameof(Dashboard));
            }

            var normalizedDecision = (input.Decision ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedDecision != "accept" && normalizedDecision != "reject")
            {
                TempData["DashboardError"] = "Unknown bulk action.";
                return RedirectToAction(nameof(Dashboard));
            }

            _logger.LogInformation(
                "Supplier queued bulk {Decision} for requests: {Ids}",
                normalizedDecision,
                string.Join(",", input.SelectedRequestIds));

            TempData["DashboardMessage"] = $"Bulk {normalizedDecision} submitted for {input.SelectedRequestIds.Length} request(s).";
            return RedirectToAction(nameof(Dashboard));
        }

        private SupplierDashboardViewModel CreateSampleDashboard()
        {
            var now = DateTime.UtcNow;

            var incomingRequests = new List<IncomingRequestGroupViewModel>
            {
                new()
                {
                    BuyerId = 301,
                    BuyerName = "Northwind Purchasing",
                    Requests = new List<IncomingRequestItemViewModel>
                    {
                        new()
                        {
                            RequestId = 4101,
                            Reference = "PR-4101",
                            EstimatedTotal = 2450m,
                            DateSubmitted = now.AddDays(-1),
                            Status = PRStatusType.PendingApproval,
                            NeededBy = DateOnly.FromDateTime(now.AddDays(6))
                        },
                        new()
                        {
                            RequestId = 4102,
                            Reference = "PR-4102",
                            EstimatedTotal = 1680m,
                            DateSubmitted = now.AddDays(-3),
                            Status = PRStatusType.PendingApproval,
                            NeededBy = DateOnly.FromDateTime(now.AddDays(4))
                        }
                    }
                },
                new()
                {
                    BuyerId = 302,
                    BuyerName = "Contoso Manufacturing",
                    Requests = new List<IncomingRequestItemViewModel>
                    {
                        new()
                        {
                            RequestId = 4203,
                            Reference = "PR-4203",
                            EstimatedTotal = 5120m,
                            DateSubmitted = now.AddDays(-2),
                            Status = PRStatusType.PendingApproval,
                            NeededBy = DateOnly.FromDateTime(now.AddDays(7))
                        }
                    }
                }
            };

            var activeOrders = new List<ActiveOrderViewModel>
            {
                new()
                {
                    PurchaseOrderId = 5051,
                    Reference = "PO-5051",
                    EstimatedDeliveryDate = DateOnly.FromDateTime(now.AddDays(5)),
                    PaymentStatus = PaymentStatusType.PartiallyPaid,
                    FulfillmentStatus = POStatusType.PartiallyReceived,
                    TotalAmount = 7825m,
                    IsDelivered = false,
                    IsPaymentComplete = false
                },
                new()
                {
                    PurchaseOrderId = 5052,
                    Reference = "PO-5052",
                    EstimatedDeliveryDate = DateOnly.FromDateTime(now.AddDays(2)),
                    PaymentStatus = PaymentStatusType.NotPaid,
                    FulfillmentStatus = POStatusType.Issued,
                    TotalAmount = 3120m,
                    IsDelivered = false,
                    IsPaymentComplete = false
                }
            };

            var acceptedHistory = new List<AcceptedHistoryItemViewModel>
            {
                new()
                {
                    Reference = "PO-4988",
                    Amount = 12450m,
                    AcceptedOn = DateOnly.FromDateTime(now.AddDays(-14)),
                    BuyerName = "Northwind Purchasing",
                    PaymentStatus = PaymentStatusType.Paid,
                    FulfillmentStatus = POStatusType.Closed,
                    DeliveryDate = DateOnly.FromDateTime(now.AddDays(-7)),
                    InvoiceNumber = "INV-8891",
                    InvoiceDate = DateOnly.FromDateTime(now.AddDays(-16)),
                    PaymentDueDate = DateOnly.FromDateTime(now.AddDays(-9)),
                    PaymentDate = DateOnly.FromDateTime(now.AddDays(-8)),
                    InvoiceAmount = 12450m
                },
                new()
                {
                    Reference = "PO-4991",
                    Amount = 9860m,
                    AcceptedOn = DateOnly.FromDateTime(now.AddDays(-9)),
                    BuyerName = "Contoso Manufacturing",
                    PaymentStatus = PaymentStatusType.Paid,
                    FulfillmentStatus = POStatusType.Closed,
                    DeliveryDate = DateOnly.FromDateTime(now.AddDays(-4)),
                    InvoiceNumber = "INV-8920",
                    InvoiceDate = DateOnly.FromDateTime(now.AddDays(-11)),
                    PaymentDueDate = DateOnly.FromDateTime(now.AddDays(-6)),
                    PaymentDate = DateOnly.FromDateTime(now.AddDays(-5)),
                    InvoiceAmount = 9860m
                },
                new()
                {
                    Reference = "PO-4999",
                    Amount = 4350m,
                    AcceptedOn = DateOnly.FromDateTime(now.AddDays(-6)),
                    BuyerName = "Fabrikam Labs",
                    PaymentStatus = PaymentStatusType.Paid,
                    FulfillmentStatus = POStatusType.Closed,
                    DeliveryDate = DateOnly.FromDateTime(now.AddDays(-2)),
                    InvoiceNumber = "INV-8975",
                    InvoiceDate = DateOnly.FromDateTime(now.AddDays(-8)),
                    PaymentDueDate = DateOnly.FromDateTime(now.AddDays(-3)),
                    PaymentDate = DateOnly.FromDateTime(now.AddDays(-2)),
                    InvoiceAmount = 4350m
                }
            };

            var revenueMix = acceptedHistory
                .GroupBy(item => item.BuyerName)
                .Select(group => new RevenueSliceViewModel
                {
                    Label = group.Key,
                    Amount = group.Sum(item => item.Amount)
                })
                .OrderByDescending(slice => slice.Amount)
                .ToList();

            return new SupplierDashboardViewModel
            {
                IncomingRequests = incomingRequests,
                ActiveOrders = activeOrders,
                AcceptedHistory = acceptedHistory,
                RevenueMix = revenueMix,
                Performance = SupplierPerformanceViewModel.Placeholder()
            };
        }

        // GET: Supplier/Upload
        public IActionResult Upload()
        {
            ViewBag.UserType = "Supplier";
            return View();
        }

        // POST: Supplier/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ProductUploadViewModel model)
        {
            ViewBag.UserType = "Supplier";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                const int supplierId = 1; // TODO: Replace with authenticated supplier id from session/claims

                // Create new product entity FIRST to get the ProductID
                var product = new GBazaar.Models.Product
                {
                    SupplierID = supplierId,
                    ProductName = model.ProductName.Trim(),
                    Description = model.Description?.Trim(),
                    UnitPrice = model.UnitPrice,
                    UnitOfMeasure = model.UnitOfMeasure.Trim()
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(); // This assigns the ProductID

                // NOW handle image upload using the ProductID
                if (model.ProductImage != null && model.ProductImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                    Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

                    // Get file extension from uploaded file
                    var fileExtension = Path.GetExtension(model.ProductImage.FileName);
                    
                    // Name the file using ProductID
                    var fileName = $"{product.ProductID}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Save the image file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProductImage.CopyToAsync(fileStream);
                    }

                    _logger.LogInformation("Product image saved as: {FileName} for ProductID: {ProductId}", fileName, product.ProductID);
                }

                _logger.LogInformation("Product created successfully: {ProductName} (ID: {ProductId})", product.ProductName, product.ProductID);
                TempData["UploadSuccess"] = $"Product '{product.ProductName}' has been added successfully! (ID: {product.ProductID})";
                
                return RedirectToAction(nameof(Upload));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload product.");
                ModelState.AddModelError(string.Empty, "An error occurred while uploading the product. Please try again.");
                return View(model);
            }
        }

        private static bool IsDashboardConnectivityIssue(Exception exception)
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
