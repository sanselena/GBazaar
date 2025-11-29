using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        [Required]
        [StringLength(200)]
        public string SupplierName { get; set; }

        [StringLength(100)]
        public string? ContactName { get; set; }

        [StringLength(100)] 
        public string? ContactInfo { get; set; }

        [StringLength(20)]
        public string? TaxID { get; set; }

        public int PaymentTermID { get; set; }

        public virtual PaymentTerm PaymentTerm { get; set; }

        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<PRItem> PRItems { get; set; } = new List<PRItem>();
        public virtual ICollection<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
