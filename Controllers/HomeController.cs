using Gbazaar.Data;
using GBazaar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GBazaar.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProcurementContext _context;
        private static readonly Random _random = new Random();

        public HomeController(ILogger<HomeController> logger, ProcurementContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is a logged-in supplier and redirect to their homepage
            if (User.Identity.IsAuthenticated)
            {
                var userType = User.FindFirst("UserType")?.Value;
                if (userType == "Supplier")
                {
                    return RedirectToAction("HomepageS", "Supplier");
                }
            }

            // 1. Veritabanından sadece ürün ID'lerini al
            var allProductIds = await _context.Products
                .Select(p => p.ProductID)
                .ToListAsync();

            // 2. ID listesini bellekte karıştır ve 12 tane seç
            var randomIds = allProductIds
                .OrderBy(id => _random.Next())
                .Take(12)
                .ToList();

            // 3. Sadece seçilen ID'lere sahip ürünleri veritabanından çek
            var products = await _context.Products
                .Where(p => randomIds.Contains(p.ProductID))
                .Include(p => p.Supplier)
                .AsNoTracking()
                .ToListAsync();

            // 4. Son listeyi de rastgele sıralamak için (isteğe bağlı ama önerilir)
            var finalProducts = products
                .OrderBy(p => randomIds.IndexOf(p.ProductID))
                .ToList();

            // Ürünleri view'a model olarak gönder
            return View(finalProducts);
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