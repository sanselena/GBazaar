using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class SupplierRating
    {
        [Key]
        public int RatingID { get; set; }
        [Required]
        public int POID { get; set; }
        [Required]
        public int RatedByUserID { get; set; }

        [Required]
        public int RatingScore { get; set; } 

        [Column(TypeName = "nvarchar(max)")]
        public string? FeedBack { get; set; }

        public virtual PurchaseOrder PurchaseOrder { get; set; }
        public virtual User Rater { get; set; }

    }
}
