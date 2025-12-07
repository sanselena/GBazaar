using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.Enums;
using GBazaar.ViewModels.Supplier;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
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

        // po oluştur
        private void CreatePurchaseOrder(PurchaseRequest pr)
        {
            var po = new PurchaseOrder
            {
                PRID = pr.PRID,
                SupplierID = pr.SupplierID ?? throw new InvalidOperationException("Supplier ID is required"),
                DateIssued = DateTime.UtcNow,

                // po awaitin sup 
                POStatus = Models.Enums.POStatusType.PendingSupplierApproval,
                POStatusID = (int)Models.Enums.POStatusType.PendingSupplierApproval,

                RequiredDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))
            };

            // pr itemi po iteme çek
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

            pr.PRStatus = PRStatusType.AwaitingSupplier;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.UserType = "Supplier";

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var supplierId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userType = User.FindFirst("UserType")?.Value;
            if (userType != "Supplier")
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                _context.ChangeTracker.Clear();

                // inc req
                var incomingPOs = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Where(po => po.SupplierID == supplierId && po.POStatus == POStatusType.PendingSupplierApproval)
                    .Include(po => po.PurchaseRequest)
                        .ThenInclude(pr => pr.Requester)
                    .Include(po => po.POItems)
                    .OrderByDescending(po => po.DateIssued)
                    .ToListAsync();

                var groupedRequests = incomingPOs
                    .GroupBy(po => po.PurchaseRequest?.Requester)
                    .Select(group => new IncomingRequestGroupViewModel
                    {
                        BuyerId = group.Key?.UserID ?? 0,
                        BuyerName = group.Key?.FullName ?? "Unknown Buyer",
                        Requests = group
                            .Select(po => new IncomingRequestItemViewModel
                            {
                                RequestId = po.POID,
                                Reference = $"PO-{po.POID:0000}",
                                EstimatedTotal = po.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice),
                                DateSubmitted = po.DateIssued,
                                Status = PRStatusType.PendingApproval,
                                NeededBy = po.RequiredDeliveryDate ?? DateOnly.FromDateTime(po.DateIssued.AddDays(14))
                            })
                            .ToList()
                    })
                    .ToList();

                // tüm sup order
                var allSupplierPOs = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Where(po => po.SupplierID == supplierId &&
                                po.POStatus != POStatusType.Rejected &&
                                po.POStatus != POStatusType.PendingSupplierApproval) 
                    .Include(po => po.PurchaseRequest)
                        .ThenInclude(pr => pr.Requester)
                    .Include(po => po.Invoices)
                    .Include(po => po.POItems)
                    .OrderByDescending(po => po.DateIssued)
                    .ToListAsync();

                var activeOrders = new List<ActiveOrderViewModel>();
                var closedOrders = new List<ClosedOrderViewModel>();

                foreach (var po in allSupplierPOs)
                {
                    var latestInvoice = po.Invoices
                        .OrderByDescending(i => i.InvoiceDate)
                        .ThenByDescending(i => i.InvoiceID)
                        .FirstOrDefault();

                    var paymentStatus = latestInvoice?.PaymentStatus ?? PaymentStatusType.NotPaid;
                    var totalAmount = po.POItems.Sum(item => item.QuantityOrdered * item.UnitPrice);
                    var isDelivered = po.POStatus == POStatusType.FullyReceived || po.POStatus == POStatusType.Closed;
                    var hasInvoice = po.Invoices.Any();

                    // invoice varsa closed
                    if (hasInvoice)
                    {
                        closedOrders.Add(new ClosedOrderViewModel
                        {
                            Reference = $"PO-{po.POID:0000}",
                            Amount = totalAmount,
                            BuyerName = po.PurchaseRequest?.Requester?.FullName ?? "Unknown Buyer",
                            AcceptedOn = DateOnly.FromDateTime(po.DateIssued),
                            PaymentStatus = paymentStatus,
                            FulfillmentStatus = po.POStatus,
                            DeliveryDate = po.RequiredDeliveryDate,
                            InvoiceNumber = latestInvoice?.InvoiceNumber ?? string.Empty,
                            InvoiceDate = latestInvoice?.InvoiceDate,
                            PaymentDueDate = latestInvoice?.DueDate,
                            PaymentDate = latestInvoice?.PaymentDate,
                            InvoiceAmount = latestInvoice?.AmountDue ?? totalAmount,
                            CompletedDate = latestInvoice?.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                            PurchaseOrderId = po.POID,
                            InvoiceId = latestInvoice?.InvoiceID
                        });
                    }
                    // invoice yoksa active
                    else if (!isDelivered)
                    {
                        activeOrders.Add(new ActiveOrderViewModel
                        {
                            PurchaseOrderId = po.POID,
                            Reference = $"PO-{po.POID:0000}",
                            EstimatedDeliveryDate = po.RequiredDeliveryDate,
                            PaymentStatus = paymentStatus,
                            FulfillmentStatus = po.POStatus,
                            TotalAmount = totalAmount,
                            IsDelivered = isDelivered,
                            IsPaymentComplete = paymentStatus == PaymentStatusType.Paid
                        });
                    }
                }

                var finalClosedOrders = closedOrders
                    .OrderByDescending(item => item.AcceptedOn)
                    .Take(10)
                    .ToList();

                // revenue closeda göre
                var revenueMix = closedOrders
                    .GroupBy(item => item.BuyerName)
                    .Select(group => new RevenueSliceViewModel
                    {
                        Label = group.Key,
                        Amount = group.Sum(item => item.Amount)
                    })
                    .Where(slice => slice.Amount > 0) 
                    .OrderByDescending(slice => slice.Amount)
                    .ToList();

                // rating hesapla
                var supplierRatings = await _context.SupplierRatings
                    .Where(sr => _context.PurchaseOrders.Any(po => po.POID == sr.POID && po.SupplierID == supplierId))
                    .Select(sr => sr.RatingScore)
                    .ToListAsync();

                var performance = supplierRatings.Any() 
                    ? SupplierPerformanceViewModel.Create(
                        averageRating: Math.Round((decimal)supplierRatings.Average(), 1),
                        totalRatings: supplierRatings.Count)
                    : SupplierPerformanceViewModel.Default();

                var viewModel = new SupplierDashboardViewModel
                {
                    IncomingRequests = groupedRequests,
                    ActiveOrders = activeOrders,
                    ClosedOrders = finalClosedOrders, 
                    RevenueMix = revenueMix,
                    Performance = performance 
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
            // Giriş yapmış supplier'ın ID'sini al
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var supplierId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // po bul
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.PurchaseRequest)
                .FirstOrDefaultAsync(po => po.POID == requestId && po.SupplierID == supplierId &&
                                   po.POStatus == POStatusType.PendingSupplierApproval);

            if (purchaseOrder == null)
            {
                TempData["DashboardError"] = "Order not found or already processed.";
                return RedirectToAction(nameof(Dashboard));
            }

            var normalizedDecision = (decision ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedDecision != "accept" && normalizedDecision != "reject")
            {
                TempData["DashboardError"] = "Unknown action.";
                return RedirectToAction(nameof(Dashboard));
            }

            try
            {
                if (normalizedDecision == "accept")
                {
                    // accept po aktif olur
                    purchaseOrder.POStatus = POStatusType.Issued;
                    purchaseOrder.POStatusID = (int)POStatusType.Issued;

                    // status güncelle
                    if (purchaseOrder.PurchaseRequest != null)
                    {
                        purchaseOrder.PurchaseRequest.PRStatus = PRStatusType.Ordered;
                    }

                    TempData["DashboardMessage"] = $"Order PO-{purchaseOrder.POID:0000} has been accepted and is now active.";
                    _logger.LogInformation("Supplier {SupplierId} accepted PO-{OrderId}", supplierId, purchaseOrder.POID);
                }
                else 
                {
                    // reject poyu geri yollar
                    purchaseOrder.POStatus = POStatusType.Rejected;
                    purchaseOrder.POStatusID = (int)POStatusType.Rejected;

                    // status güncelle
                    if (purchaseOrder.PurchaseRequest != null)
                    {
                        purchaseOrder.PurchaseRequest.PRStatus = PRStatusType.Rejected;
                    }

                    TempData["DashboardMessage"] = $"Order PO-{purchaseOrder.POID:0000} has been rejected.";
                    _logger.LogInformation("Supplier {SupplierId} rejected PO-{OrderId}", supplierId, purchaseOrder.POID);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing decision for PO-{OrderId}", purchaseOrder.POID);
                TempData["DashboardError"] = "An error occurred while processing your decision.";
            }

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

            //  Sample ClosedOrders 
            var closedOrders = new List<ClosedOrderViewModel>
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
                    InvoiceAmount = 12450m,
                    CompletedDate = DateTime.UtcNow.AddDays(-16),
                    PurchaseOrderId = 4988,
                    InvoiceId = 8891
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
                    InvoiceAmount = 9860m,
                    CompletedDate = DateTime.UtcNow.AddDays(-11),
                    PurchaseOrderId = 4991,
                    InvoiceId = 8920
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
                    InvoiceAmount = 4350m,
                    CompletedDate = DateTime.UtcNow.AddDays(-8),
                    PurchaseOrderId = 4999,
                    InvoiceId = 8975
                }
            };

            var revenueMix = closedOrders
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
                ClosedOrders = closedOrders, 
                RevenueMix = revenueMix,
                Performance = SupplierPerformanceViewModel.Create(4.2m, 15) 
            };
        }

        public async Task<IActionResult> HomepageS()
        {
            ViewBag.UserType = "Supplier";

            // Get the logged-in supplier's ID
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var supplierId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userType = User.FindFirst("UserType")?.Value;
            if (userType != "Supplier")
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // Get all products for this specific supplier
                var supplierProducts = await _context.Products
                    .Where(p => p.SupplierID == supplierId)
                    .Include(p => p.Supplier)
                    .AsNoTracking()
                    .ToListAsync();

                // shuffle
                if (Request.Query.ContainsKey("refresh"))
                {
                    var random = new Random();
                    supplierProducts = supplierProducts.OrderBy(x => random.Next()).ToList();
                }

                return View(supplierProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load supplier products for supplier ID {SupplierId}", supplierId);
                TempData["Error"] = "Unable to load your products. Please try again later.";
                return View(new List<Product>());
            }
        }
       
        public IActionResult Upload()
        {
            ViewBag.UserType = "Supplier";
            return View();
        }

        // sup add
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
                // logged in sup id
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var supplierId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // create prod
                var product = new GBazaar.Models.Product
                {
                    SupplierID = supplierId,
                    ProductName = model.ProductName.Trim(),
                    Description = model.Description?.Trim(),
                    UnitPrice = model.UnitPrice,
                    UnitOfMeasure = model.UnitOfMeasure.Trim()
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(); // give prodid

                // img upload
                if (model.ProductImage != null && model.ProductImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                    Directory.CreateDirectory(uploadsFolder); 

                    var fileExtension = Path.GetExtension(model.ProductImage.FileName);

                    // name file as proid
                    var fileName = $"{product.ProductID}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // save img
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