using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBazaar.Models
{
    public class Attachment
    {
        [Key]
        public int AttachmentID { get; set; }
        [Required]
        public int EntityID { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; }

        [Required]
        [StringLength(250)]
        public string FileName { get; set; } = string.Empty;
        public int UploadedByUserID { get; set; }
        [Required]
        public virtual User Uploader { get; set; }
    }
}
