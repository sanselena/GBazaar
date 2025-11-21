using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class GoodsReceipt
    {
        [Key]
        public int ReceiptID { get; set; }

        // FKs
        public int POID { get; set; }
        public int ReceivedByUserID { get; set; }

        [Required]
        public DateTime DateReceived { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal QuantityReceived { get; set; }

        // Navigation Properties
        [ForeignKey("POID")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("ReceivedByUserID")]
        public virtual User? Receiver { get; set; }
    }
}
