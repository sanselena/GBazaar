using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public int SupplierID { get; set; }
        [ForeignKey(nameof(SupplierID))]
        public virtual Supplier Supplier { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitPrice { get; set; }

        [StringLength(50)]
        public string? UnitOfMeasure { get; set; }

        public virtual ICollection<PRItem> PRItems { get; set; } = new List<PRItem>();
        public virtual ICollection<POItem> POItems { get; set; } = new List<POItem>();


    }
}