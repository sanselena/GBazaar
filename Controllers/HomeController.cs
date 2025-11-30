using System.Diagnostics;
using System.Net.Sockets;
using GBazaar.Models;
using GBazaar.ViewModels.Home;
using Gbazaar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace GBazaar.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProcurementContext _context;

        private static readonly TimeSpan CatalogQueryTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan CatalogRetryCooldown = TimeSpan.FromMinutes(5);
        private static readonly object CatalogStateGate = new();
        private static DateTimeOffset _catalogRetryAfterUtc = DateTimeOffset.MinValue;

        public HomeController(ILogger<HomeController> logger, ProcurementContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTimeOffset.UtcNow;
            bool shouldAttemptCatalog;

            lock (CatalogStateGate)
            {
                shouldAttemptCatalog = now >= _catalogRetryAfterUtc;
            }

            List<ProductCardViewModel>? cards = null;

            if (shouldAttemptCatalog)
            {
                try
                {
                    using var cts = new CancellationTokenSource(CatalogQueryTimeout);

                    var products = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Supplier)
                        .ToListAsync(cts.Token);

                    if (products.Any())
                    {
                        cards = ShuffleCards(products.Select(p => new ProductCardViewModel
                        {
                            ProductId = p.ProductID,
                            ProductName = p.ProductName,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "Unknown Supplier",
                            UnitPrice = p.UnitPrice,
                            UnitOfMeasure = p.UnitOfMeasure,
                            Description = p.Description,
                            ImageUrl = null
                        }));
                    }

                    lock (CatalogStateGate)
                    {
                        _catalogRetryAfterUtc = DateTimeOffset.MinValue;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Falling back to sample products because the catalog query timed out.");
                    RegisterCatalogFailure(now, CatalogRetryCooldown);
                }
                catch (Exception ex) when (IsCatalogConnectivityIssue(ex))
                {
                    _logger.LogWarning(ex, "Falling back to sample products because the catalog database is unreachable.");
                    RegisterCatalogFailure(now, CatalogRetryCooldown);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load catalog for home page.");
                    RegisterCatalogFailure(now, TimeSpan.FromSeconds(30));
                }
            }

            if (cards is null || !cards.Any())
            {
                ViewBag.UsingSampleProducts = true;
                return View(ShuffleCards(CreateSampleCatalog()));
            }

            ViewBag.UsingSampleProducts = false;
            return View(cards);
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

        private static void RegisterCatalogFailure(DateTimeOffset now, TimeSpan delay)
        {
            lock (CatalogStateGate)
            {
                _catalogRetryAfterUtc = now.Add(delay);
            }
        }

        private static bool IsCatalogConnectivityIssue(Exception ex)
        {
            if (ex is SqlException || ex is SocketException)
            {
                return true;
            }

            if (ex.InnerException is not null)
            {
                return IsCatalogConnectivityIssue(ex.InnerException);
            }

            return false;
        }

        private static List<ProductCardViewModel> CreateSampleCatalog()
        {
            return new List<ProductCardViewModel>
            {
                new()
                {
                    ProductId = 1,
                    ProductName = "Organic Tomatoes (Sample)",
                    SupplierName = "FarmFresh Anatolia",
                    UnitPrice = 1.20m,
                    UnitOfMeasure = "kg",
                    Description = "Sun-ripened tomatoes ready for salad prep.",
                    ImageUrl = null
                },
                new()
                {
                    ProductId = 2,
                    ProductName = "Cool T-Shirt (Sample)",
                    SupplierName = "Cotton Co.",
                    UnitPrice = 19.99m,
                    UnitOfMeasure = null,
                    Description = "Pre-shrunk cotton tee with unisex fit.",
                    ImageUrl = null
                }
            };
        }

        private static List<ProductCardViewModel> ShuffleCards(IEnumerable<ProductCardViewModel> source)
        {
            var cards = source.ToList();

            for (var i = cards.Count - 1; i > 0; i--)
            {
                var j = Random.Shared.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }

            return cards;
        }
    }
}
