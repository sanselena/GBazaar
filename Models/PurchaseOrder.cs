using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;

namespace GBazaar.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int POID { get; set; }

        // FK (and UNIQUE constraint from SQL)
        public int PRID { get; set; }

        // FK
        public int SupplierID { get; set; }

        [Required]
        public DateTime DateIssued { get; set; }

        public DateOnly? RequiredDeliveryDate { get; set; } // Using DateOnly for 'date' type

        // FK
        public int POStatusID { get; set; }

        // Navigation Properties
        [ForeignKey("PRID")]
        public virtual PurchaseRequest? PurchaseRequest { get; set; }

        [ForeignKey("SupplierID")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("POStatusID")]
        public virtual POStatus? POStatus { get; set; }
    }
}
