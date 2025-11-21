using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class SupplierRating
    {
        [Key]
        public int RatingID { get; set; }

        // FKs
        public int POID { get; set; }
        public int RatedByUserID { get; set; }

        [Required]
        public int RatingScore { get; set; } // Assuming 1-5 score

        [Column(TypeName = "nvarchar(max)")]
        public string? FeedBack { get; set; }

        // Navigation Properties
        [ForeignKey("POID")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("RatedByUserID")]
    }
}
