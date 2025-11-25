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
        public DateTime RatedOn { get; set; } = DateTime.UtcNow;

        [Required]
        [Range(1, 5)]
        public int RatingScore { get; set; } 

        [Column(TypeName = "nvarchar(max)")]
        public string? FeedBack { get; set; }

        [ForeignKey(nameof(POID))]
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        [ForeignKey(nameof(RatedByUserID))] 
        public virtual User Rater { get; set; }

    }
}
