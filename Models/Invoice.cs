using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceID { get; set; }

        // FKs
        public int POID { get; set; }
        public int SupplierID { get; set; }
        public int? PaymentStatusID { get; set; } // Allows NULL from SQL (no NOT NULL constraint)

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public DateOnly InvoiceDate { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AmountDue { get; set; }

        // Navigation Properties
        [ForeignKey("POID")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("SupplierID")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("PaymentStatusID")]
        public virtual PaymentStatus? PaymentStatus { get; set; }
    }
}
