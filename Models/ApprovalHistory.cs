using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GBazaar.Models.Enums;

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
        public ApprovalActionType ActionType { get; set; } 

        [Required]
        public int ApprovalLevel { get; set; }

        public string? Notes { get; set; }

        [ForeignKey(nameof(PRID))] 
        public virtual PurchaseRequest PurchaseRequest { get; set; }
        [ForeignKey(nameof(ApproverID))] 
        public virtual User Approver { get; set; }
    }
}
