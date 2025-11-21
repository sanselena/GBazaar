using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BudgetCode { get; set; } = string.Empty;
    }
}
