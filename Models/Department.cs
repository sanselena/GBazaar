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

        [ Required]
        [StringLength(50)]
        public string BudgetCode { get; set; }
        public int? ManagerID { get; set; }
        public virtual User? Manager { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    }
}
