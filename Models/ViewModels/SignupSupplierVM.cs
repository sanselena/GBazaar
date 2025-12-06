using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models.ViewModels
{
    public class SignupSupplierVM
    {
        [Required]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; } = string.Empty;

        [Display(Name = "Contact Person")]
        public string? ContactName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Official Contact Email")]
        public string ContactInfo { get; set; } = string.Empty;

        [Display(Name = "Tax ID (VKN)")]
        public string? TaxId { get; set; }

        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Payment Terms")]
        public int PaymentTermID { get; set; }

        [Required]
        public bool AcceptTerms { get; set; }
    }
}