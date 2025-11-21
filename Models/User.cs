using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
namespace GBazaar.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string PAsswordHash { get; set; } = string.Empty; // Note: Matched your SQL spelling

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        // Foreign Keys (FKs)
        public int RoleID { get; set; }
        public int? DepartmentID { get; set; } // Matches your SQL allowing NULL

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey("RoleID")]
        public virtual Role? Role { get; set; }

        [ForeignKey("DepartmentID")]
        public virtual Department? Department { get; set; }
    }
}
