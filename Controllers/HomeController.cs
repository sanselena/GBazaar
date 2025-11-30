using System.Diagnostics;
using Gbazaar.Data;
using GBazaar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GBazaar.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProcurementContext _context;

        public HomeController(ProcurementContext context){
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.ProductID)
                .ToList();
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
