using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; }

        [Required]
        [StringLength(50)]
        public string BudgetCode { get; set; }
        public Budget? Budget { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();

    }
}
