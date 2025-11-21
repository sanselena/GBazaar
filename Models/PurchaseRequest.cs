using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;

namespace GBazaar.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int PRID { get; set; }

        // FK
        public int RequesterID { get; set; }

        [Required]
        public DateTime DateSubmitted { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal EstimatedTotal { get; set; }

        // FK
        public int PRStatusID { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Justification { get; set; }

        // Navigation Properties
        [ForeignKey("RequesterID")]
        public virtual User? Requester { get; set; }

        [ForeignKey("PRStatusID")]
        public virtual PRStatus? PRStatus { get; set; }

        public ICollection<PRItem>? PRItems { get; set; }
    }
}
