using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class Budget
    {
        [Key]
        public int BudgetID { get; set; }
        public int DepartmentID { get; set; }
        [Required]
        public int FiscalYear { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalBudget { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AmountCommitted { get; set; }

        [ForeignKey("DepartmentID")] 
        public virtual Department Department { get; set; }
    }
}
