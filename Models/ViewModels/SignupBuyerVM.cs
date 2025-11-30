using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models.ViewModels
{
    public class SignupBuyerVM
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string CompanyName { get; set; }

        [Required]
        public string Department { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }

}