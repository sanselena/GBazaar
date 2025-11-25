using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class PRItem
    {
        [Key]
        public int PRItemID { get; set; }

        [Required]
        public int PRID { get; set; }

        [Required]
        [StringLength(200)]
        public string PRItemName { get; set; }

        [Required]
        [StringLength(250)]

        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string UnitOfMeasure { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitPrice { get; set; }

        [Required]
        public virtual PurchaseRequest PurchaseRequest { get; set; }
    }
}
