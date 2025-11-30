using System.Threading;
using System.Threading.Tasks;
using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.ViewModels;
using GBazaar.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult CreatePurchaseRequest(PRVM model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", new { id = model.ProductId });
            }

            var product = _context.Products
                .Include(p => p.Supplier)
                .FirstOrDefault(p => p.ProductID == model.ProductId);

            if (product == null)
                return NotFound();

            PurchaseRequest pr;

            if (model.PRID == 0)
            {
                 pr = new PurchaseRequest
                {
                    RequesterID = model.BuyerId,
                    SupplierID = product.SupplierID,
                    DateSubmitted = DateTime.Now,
                    PRStatus = Models.Enums.PRStatusType.PendingApproval,
                    PRStatusID = (int)Models.Enums.PRStatusType.PendingApproval,
                    EstimatedTotal = 0 //item eklenince assa yazdim
                };

                _context.PurchaseRequests.Add(pr);
                _context.SaveChanges();
            }
            else
            {
                pr = _context.PurchaseRequests.FirstOrDefault(pr => pr.PRID == model.PRID);
                if (pr == null)
                {
                    return NotFound();
                }

                if(pr.SupplierID != product.SupplierID)
                {
                   ModelState.AddModelError("", "The selected product's supplier does not match the existing Purchase Request's supplier.");
                     return RedirectToAction("Details", new { id = model.ProductId });
                }

            }
            
                var prItem = new PRItem
                {
                    PRID = pr.PRID,
                    ProductID = model.ProductId,
                    PRItemName = model.ProductName,
                    Description = model.ProductDescription,
                    Quantity = model.Quantity,
                    UnitOfMeasure = model.UnitOfMeasure,
                    UnitPrice = model.UnitPrice,
                    SupplierID = product.SupplierID
                };

                _context.PRItems.Add(prItem);
                _context.SaveChanges();

            // esttotaalll
            pr.EstimatedTotal = (decimal)_context.PRItems
                .Where(item => item.PRID == pr.PRID)
                .Sum(item => item.Quantity * item.UnitPrice);
            _context.SaveChanges();

            return RedirectToAction("Details", new {id = model.ProductId});
        }
    }

}
