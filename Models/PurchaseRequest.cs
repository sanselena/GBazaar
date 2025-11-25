using GBazaar.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int PRID { get; set; }
        [Required] 
        public int RequesterID { get; set; }

        [Required]
        public DateTime DateSubmitted { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal EstimatedTotal { get; set; }
        [Required] 
        public int PRStatusID { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Justification { get; set; }

        [Required] 
        public virtual User Requester { get; set; }

        [Required]
        public PRStatusType PRStatus { get; set; }
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
        public virtual ICollection<PRItem> PRItems { get; set; } = new List<PRItem>();
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
    }
}
