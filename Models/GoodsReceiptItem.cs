using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace GBazaar.Models
{
    public class GoodsReceiptItem
    {
        [Key]
        public int GoodsReceiptItemID { get; set; }

        public int ReceiptID { get; set; }
        public int POItemID { get; set; }

        public decimal QuantityReceived { get; set; }

        [ForeignKey(nameof(ReceiptID))]
        public GoodsReceipt GoodsReceipt { get; set; }

        [ForeignKey(nameof(POItemID))]
        public POItem POItem { get; set; }
    }

