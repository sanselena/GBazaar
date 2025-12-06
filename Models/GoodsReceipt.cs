using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class GoodsReceipt
    {
        [Key]
        public int ReceiptID { get; set; }
        [Required]
        public int POID { get; set; }
        [Required]
        public int ReceivedByUserID { get; set; }

        [Required]
        public DateTime DateReceived { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal QuantityReceived { get; set; }

        [Required]
        [ForeignKey(nameof(POID))]
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        [Required]
        [ForeignKey(nameof(ReceivedByUserID))]
        public virtual User Receiver { get; set; }
        public virtual ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();
    }
}
