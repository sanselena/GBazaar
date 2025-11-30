namespace GBazaar.Models.ViewModels
{
    public class PRVM
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
       // public decimal OfferPrice { get; set; }
        public int BuyerId { get; set; }
        public string Note { get; set; }
        public int SupplierId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string UnitOfMeasure { get; set; }
        public int PRID { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
