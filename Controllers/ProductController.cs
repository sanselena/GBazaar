using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.ViewModels;
using GBazaar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace GBazaar.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProcurementContext _context;
        private readonly ILogger<ProductController> _logger;
        private static readonly TimeSpan ProductQueryTimeout = TimeSpan.FromSeconds(2);

        public ProductController(ProcurementContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        //tiklayinca acilcak
        public async Task<IActionResult> Details(int id)
        {
            Product? product = null;
            var shouldUseFallback = false;

            try
            {
                using var cts = new CancellationTokenSource(ProductQueryTimeout);

                product = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductID == id, cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Timed out retrieving product {ProductId}. Falling back to sample data.", id);
                shouldUseFallback = true;
            }
            catch (Exception ex) when (CatalogFallbackService.IsCatalogConnectivityIssue(ex))
            {
                _logger.LogWarning(ex, "Unable to reach catalog for product {ProductId}. Falling back to sample data.", id);
                shouldUseFallback = true;
            }

            if (product != null)
            {
                ViewBag.UsingSampleProduct = false;
                return View(product);
            }

            if (shouldUseFallback && CatalogFallbackService.TryCreateSampleProduct(id, out var sample))
            {
                ViewBag.UsingSampleProduct = true;
                return View(sample);
            }

            if (shouldUseFallback)
            {
                _logger.LogWarning("No sample fallback available for product {ProductId}.", id);
            }

            return NotFound();
        }

        //pr olusturma
        [HttpPost]
        [Authorize] // Bu metoda sadece giriş yapmış kullanıcılar erişebilsin
        public IActionResult CreatePurchaseRequest(PRVM model)
        {
            // 1. Giriş yapmış kullanıcının ID'sini al
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var buyerId))
            {
                // Kullanıcı bulunamazsa login sayfasına yönlendir
                return RedirectToAction("Login", "Auth");
            }

            // 2. Model geçerliliğini kontrol et
            if (!ModelState.IsValid)
            {
                // Hatanın ne olduğunu bul ve daha spesifik bir mesaj göster
                string errorMsg = "Please ensure all required fields are filled correctly. ";
                if (model.Quantity <= 0) errorMsg += "Quantity must be greater than zero. ";
                if (model.UnitPrice <= 0) errorMsg += "Unit price must be greater than zero. ";

                TempData["Error"] = errorMsg;
                return RedirectToAction("Details", new { id = model.ProductId });
            }

            var product = _context.Products.AsNoTracking().FirstOrDefault(p => p.ProductID == model.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                // 3. Yeni bir PurchaseRequest nesnesi oluştur
                var purchaseRequest = new PurchaseRequest
                {
                    RequesterID = buyerId,
                    SupplierID = product.SupplierID,
                    DateSubmitted = DateTime.UtcNow,
                    PRStatus = Models.Enums.PRStatusType.Draft, 
                    PRStatusID = (int)Models.Enums.PRStatusType.Draft, 
                    Justification = model.Justification,
                    EstimatedTotal = model.Quantity * model.UnitPrice
                };

                // 4. PRItem'ı oluştur ve ekle
                var prItem = new PRItem
                {
                    ProductID = model.ProductId,
                    PRItemName = model.ProductName,
                    Description = model.ProductDescription,
                    Quantity = model.Quantity,
                    UnitOfMeasure = model.UnitOfMeasure,
                    UnitPrice = model.UnitPrice,
                    SupplierID = product.SupplierID
                };

                purchaseRequest.PRItems.Add(prItem);
                _context.PurchaseRequests.Add(purchaseRequest);
                _context.SaveChanges();

                // 5. Approval chain başlat
                return RedirectToAction("Submit", "Approval", new { id = purchaseRequest.PRID });

                // Eski kod: TempData["Success"] = $"Purchase request for '{product.ProductName}' created successfully!";
                // Eski kod: return RedirectToAction("Details", new { id = model.ProductId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a purchase request for product {ProductId}.", model.ProductId);
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction("Details", new { id = model.ProductId });
            }
        }

    }
}
