using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GBazaar.Models.Enums;

namespace GBazaar.Models
{
    public class Permission
    {
        [Key]
        public int PermissionID { get; set; }
        [Required]
        public string PermissionName { get; set; } = null!;
        
        [Required]
        public string? Description { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
