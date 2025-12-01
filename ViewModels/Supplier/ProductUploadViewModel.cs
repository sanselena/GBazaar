using System.ComponentModel.DataAnnotations;

namespace GBazaar.ViewModels.Supplier
{
    public class ProductUploadViewModel
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Product Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Unit price must be between 0.01 and 999,999.99")]
        [Display(Name = "Unit Price (₺)")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Unit of measure is required")]
        [StringLength(50, ErrorMessage = "Unit of measure cannot exceed 50 characters")]
        [Display(Name = "Unit of Measure")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Display(Name = "Product Image")]
        public IFormFile? ProductImage { get; set; }
    }
}

// Sevgili Fulya Hanım'a notlar:
// Fotoğraf kaydetme logic'i:
// 1. Product oluştur ve DB'ye ekle
// 2. SaveChanges() → ProductID otomatik atanır
// 3. Fotoğrafı {ProductID}.{extension} olarak kaydet
// Örnek: ProductID=5 ve dosya "tomato.jpg" ise → "5.jpg" olarak kaydedilir