using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class PRItem
    {
        [Key]
        public int PRItemID { get; set; }

        // FK
        public int PRID { get; set; }

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitPrice { get; set; }

        // FK
        public int SubCategoryID { get; set; }

        // Navigation Properties
        [ForeignKey("PRID")]
        public virtual PurchaseRequest? PurchaseRequest { get; set; }

        [ForeignKey("SubCategoryID")]
        public virtual SubCategory? SubCategory { get; set; }
    }
}
