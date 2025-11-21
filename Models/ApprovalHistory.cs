using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class ApprovalHistory
    {
        [Key]
        public int HistoryID { get; set; }

        // FKs
        public int PRID { get; set; }
        public int ApproverID { get; set; }

        [Required]
        public DateTime ActionDate { get; set; }

        [Required]
        [StringLength(20)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        public int ApprovalLevel { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        // Navigation Properties
        [ForeignKey("PRID")]
        public virtual PurchaseRequest? PurchaseRequest { get; set; }

        [ForeignKey("ApproverID")]
        public virtual User? Approver { get; set; }
    }
}
