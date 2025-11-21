using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models
{
    public class PaymentTerm
    {
        [Key]
        public int PaymentTermsID { get; set; }

        [Required]
        [StringLength(100)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int DaysDue { get; set; }
    }
}
