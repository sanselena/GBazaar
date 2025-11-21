using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class ApprovalRule
    {
        [Key]
        public int RuleID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MinAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? MaxAmount { get; set; } // Allows NULL

        // FK
        public int RequiredRoleID { get; set; }

        [Required]
        public int ApprovalLevel { get; set; }

        // Navigation Property
        [ForeignKey("RequiredRoleID")]
        public virtual Role? RequiredRole { get; set; }
    }
}
