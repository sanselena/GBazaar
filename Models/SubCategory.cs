using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class SubCategory
    {
        [Key]
        public int SubCategoryID { get; set; }
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string SubCategoryName { get; set; } = string.Empty;
        public virtual Category Category { get; set; }
        public virtual ICollection<PRItem> PRItems { get; set; } = new List<PRItem>();

    }
}
