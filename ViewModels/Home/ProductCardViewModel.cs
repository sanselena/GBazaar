using System.Globalization;

namespace GBazaar.ViewModels.Home
{
    public class ProductCardViewModel
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string SupplierName { get; init; } = string.Empty;
        public decimal? UnitPrice { get; init; }
        public string? UnitOfMeasure { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }

        public string DisplayPrice()
        {
            if (UnitPrice is null)
            {
                return "Pricing on request";
            }

            var formatted = UnitPrice.Value.ToString("C", CultureInfo.CurrentCulture);
            return UnitOfMeasure is null
                ? formatted
                : $"{formatted} / {UnitOfMeasure}";
        }
    }
}
