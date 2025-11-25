using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GBazaar.Models.Enums;

namespace GBazaar.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        [Required]
        [StringLength(250)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(200)]
        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? JobTitle { get; set; }
        public int RoleID { get; set; }
        public int? DepartmentID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public virtual Role? Role { get; set; }
        public virtual Department? Department { get; set; }
        public virtual ICollection<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
        public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
        public virtual ICollection<SupplierRating> SupplierRatings { get; set; } = new List<SupplierRating>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    }   
}
