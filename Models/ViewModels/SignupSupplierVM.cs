using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models.ViewModels
{
    public class SignupSupplierVM
    {
        [Required]
        public string BusinessName { get; set; }

        [Required]
        public string TaxId { get; set; }

        [Required]
        public string ContactInfo { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        public bool AcceptTerms { get; set; }
    }

}
