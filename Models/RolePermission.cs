namespace GBazaar.Models
{
    public class RolePermission
    {
        public int RoleID { get; set; }
        public Role Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }

}
