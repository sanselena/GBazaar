using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models
{
    public class PRStatus
    {
        [Key]
        public int StatusID { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusType { get; set; }

        public virtual ICollection<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();
    }
}
