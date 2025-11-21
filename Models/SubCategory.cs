using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class SubCategory
    {
        [Key]
        public int SubCategoryID { get; set; }

        // FK
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string SubCategoryName { get; set; } = string.Empty;

        // Navigation Property
        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }
    }
}
