using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class ApprovalHistory
    {
        [Key]
        public int HistoryID { get; set; }
        [Required]
        public int PRID { get; set; }
        [Required]
        public int ApproverID { get; set; }

        [Required]
        public DateTime ActionDate { get; set; }

        [Required]
        [StringLength(20)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        public int ApprovalLevel { get; set; }

        public string? Notes { get; set; }

        public virtual PurchaseRequest PurchaseRequest { get; set; }
        public virtual User Approver { get; set; }
    }
}
