using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class POItem
    {
        [Key]
        public int POItemID { get; set; }

        [Required]
        public int POID { get; set; }

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; }  

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int QuantityOrdered { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }  

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalPrice => QuantityOrdered * UnitPrice;

        [Required]
        public virtual PurchaseOrder PurchaseOrder { get; set; }
    }
}

