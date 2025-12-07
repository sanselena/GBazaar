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
            // sup to home
            if (User.Identity.IsAuthenticated)
            {
                var userType = User.FindFirst("UserType")?.Value;
                if (userType == "Supplier")
                {
                    return RedirectToAction("HomepageS", "Supplier");
                }
            }

            var allProductIds = await _context.Products
                .Select(p => p.ProductID)
                .ToListAsync();

      //display 12 prod
            var randomIds = allProductIds
                .OrderBy(id => _random.Next())
                .Take(12)
                .ToList();

            // sadece seçilenin idsini çek
            var products = await _context.Products
                .Where(p => randomIds.Contains(p.ProductID))
                .Include(p => p.Supplier)
                .AsNoTracking()
                .ToListAsync();

            // listeyi randomizela
            var finalProducts = products
                .OrderBy(p => randomIds.IndexOf(p.ProductID))
                .ToList();

            
            return View(finalProducts);
        }

        public async Task<IActionResult> Search(string query)
        {
            ViewBag.SearchQuery = query;
            ViewBag.IsSearchResults = true;

            // user sup mu
            if (User.Identity.IsAuthenticated)
            {
                var userType = User.FindFirst("UserType")?.Value;
                if (userType == "Supplier")
                {
                    return RedirectToAction("HomepageS", "Supplier");
                }
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                // no search ,home
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // search isim sup açıklama
                var searchResults = await _context.Products
                    .Include(p => p.Supplier)
                    .Where(p => 
                        p.ProductName.Contains(query) ||
                        (p.Description != null && p.Description.Contains(query)) ||
                        (p.Supplier != null && p.Supplier.SupplierName.Contains(query)))
                    .AsNoTracking()
                    .OrderBy(p => p.ProductName)
                    .Take(50) // max 50 display
                    .ToListAsync();

                ViewBag.SearchResultsCount = searchResults.Count;
                return View("Index", searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for products with query: {Query}", query);
                ViewBag.SearchError = "An error occurred while searching. Please try again.";
                return View("Index", new List<Product>());
            }
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