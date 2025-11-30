using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using GBazaar.Models;
using GBazaar.ViewModels.Home;
using Microsoft.Data.SqlClient;

namespace GBazaar.Services
{
    /// <summary>
    /// Provides sample catalog data and shared helpers for graceful fallbacks
    /// when the real product catalog cannot be reached.
    /// </summary>
    public static class CatalogFallbackService
    {
        private sealed record SampleProductDefinition(
            int ProductId,
            int SupplierId,
            string ProductName,
            string SupplierName,
            decimal? UnitPrice,
            string? UnitOfMeasure,
            string? Description,
            string? ImageUrl);

        private static readonly IReadOnlyDictionary<int, SampleProductDefinition> SampleProducts
            = new Dictionary<int, SampleProductDefinition>
            {
                [1] = new SampleProductDefinition(
                    ProductId: 1,
                    SupplierId: 1001,
                    ProductName: "Organic Tomatoes (Sample)",
                    SupplierName: "FarmFresh Anatolia",
                    UnitPrice: 1.20m,
                    UnitOfMeasure: "kg",
                    Description: "Sun-ripened tomatoes ready for salad prep.",
                    ImageUrl: null),
                [2] = new SampleProductDefinition(
                    ProductId: 2,
                    SupplierId: 2001,
                    ProductName: "Cool T-Shirt (Sample)",
                    SupplierName: "Cotton Co.",
                    UnitPrice: 19.99m,
                    UnitOfMeasure: null,
                    Description: "Pre-shrunk cotton tee with unisex fit.",
                    ImageUrl: null)
            };

        public static List<ProductCardViewModel> CreateSampleCatalog()
        {
            return SampleProducts.Values
                .Select(definition => new ProductCardViewModel
                {
                    ProductId = definition.ProductId,
                    ProductName = definition.ProductName,
                    SupplierName = definition.SupplierName,
                    UnitPrice = definition.UnitPrice,
                    UnitOfMeasure = definition.UnitOfMeasure,
                    Description = definition.Description,
                    ImageUrl = definition.ImageUrl
                })
                .ToList();
        }

        public static bool TryCreateSampleProduct(int productId, out Product product)
        {
            if (!SampleProducts.TryGetValue(productId, out var definition))
            {
                product = null!;
                return false;
            }

            product = new Product
            {
                ProductID = definition.ProductId,
                ProductName = definition.ProductName,
                SupplierID = definition.SupplierId,
                UnitPrice = definition.UnitPrice,
                UnitOfMeasure = definition.UnitOfMeasure,
                Description = definition.Description,
                Supplier = new Supplier
                {
                    SupplierID = definition.SupplierId,
                    SupplierName = definition.SupplierName,
                    ContactName = null,
                    ContactInfo = null,
                    PaymentTermID = 0
                }
            };

            return true;
        }

        public static bool IsCatalogConnectivityIssue(Exception ex)
        {
            if (ex is SqlException or SocketException)
            {
                return true;
            }

            return ex.InnerException is not null && IsCatalogConnectivityIssue(ex.InnerException);
        }
    }
}
