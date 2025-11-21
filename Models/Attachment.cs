using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class Attachment
    {
        [Key]
        public int AttachmentID { get; set; }

        // Polymorphic Association fields (No FK in SQL, handled by logic)
        public int EntityID { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string FileName { get; set; } = string.Empty;

        // FK
        public int UploadedByUserID { get; set; }

        // Navigation Property
        [ForeignKey("UploadedByUserID")]
        public virtual User? Uploader { get; set; }
    }
}
