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
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactName { get; set; }

        [StringLength(20)]
        public string? TaxID { get; set; }

        // FK
        public int PaymentTermsID { get; set; }

        // Navigation Property
        [ForeignKey("PaymentTermsID")]
        public virtual PaymentTerm? PaymentTerms { get; set; }
    }
}
