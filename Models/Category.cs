using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        // Reverse Navigation
        public ICollection<SubCategory>? SubCategories { get; set; }
    }
}
