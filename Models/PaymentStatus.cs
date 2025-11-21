using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models
{
    public class PaymentStatus
    {
        [Key]
        public int StatusID { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusType { get; set; } = string.Empty;
    }
}
