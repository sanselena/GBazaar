using Gbazaar.Data;
using GBazaar.Models;
using GBazaar.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GBazaar.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProcurementContext _context;

        public ProductController(ProcurementContext context)
        {
            _context = context;
        }

        //tiklayinca acilcak
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Supplier)      
                //.Include(p => p.Reviews)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return NotFound();

            return View(product);
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
