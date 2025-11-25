using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GBazaar.Models.Enums;

namespace GBazaar.Models
{
    public class Attachment
    {
        [Key]
        public int AttachmentID { get; set; }

        [Required]
        public AttachmentEntityType EntityType { get; set; }

        [Required]
        public int EntityID { get; set; }  

        [Required]
        [StringLength(250)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty; 

        [StringLength(100)]
        public string? ContentType { get; set; }  

        public long? FileSizeBytes { get; set; }  

        [Required]
        public int UploadedByUserID { get; set; }

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UploadedByUserID))]
        public virtual User Uploader { get; set; }
    }
}
