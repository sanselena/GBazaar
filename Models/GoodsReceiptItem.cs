using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class GoodsReceiptItem
    {
        [Key]
        public int GRItemID { get; set; }

        [Required]
        public int GRID { get; set; }
        [ForeignKey("GRID")]
        public GoodsReceipt GoodsReceipt { get; set; }

        [Required]
        public int POItemID { get; set; }
        [ForeignKey("POItemID")]
        public POItem POItem { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal QuantityReceived { get; set; }
        public ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();

    }
}