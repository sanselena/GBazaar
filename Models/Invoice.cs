using GBazaar.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceID { get; set; }
        [Required]
        public int POID { get; set; }
        [Required]
        public int SupplierID { get; set; }
        public int? PaymentStatusID { get; set; } 

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly? DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }

        [Required]
        public DateOnly InvoiceDate { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AmountDue { get; set; }
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; }
        public PaymentStatusType? PaymentStatus { get; set; }

    }
}
