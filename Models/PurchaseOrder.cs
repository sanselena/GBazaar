using GBazaar.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int POID { get; set; }
        [Required]
        public int PRID { get; set; }

        [Required]
        public int SupplierID { get; set; }

        [Required]
        public DateTime DateIssued { get; set; }

        public DateOnly? RequiredDeliveryDate { get; set; } 

        [Required]
        public int POStatusID { get; set; }
        [Required] 
        public virtual PurchaseRequest PurchaseRequest { get; set; }
         
        public virtual Supplier Supplier { get; set; }

        [Required]
        public POStatusType POStatus { get; set; }
        public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<SupplierRating> SupplierRatings { get; set; } = new List<SupplierRating>();
    }
}
