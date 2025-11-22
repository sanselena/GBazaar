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
        [StringLength(250)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitPrice { get; set; }
        public int SubCategoryID { get; set; }
        [Required]
        public virtual PurchaseRequest PurchaseRequest { get; set; }
        [Required]
        public virtual SubCategory SubCategory { get; set; }
    }
}
