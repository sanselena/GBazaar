using System.ComponentModel.DataAnnotations;

namespace GBazaar.Models.ViewModels
{
    public class SignupBuyerVM
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        public int DepartmentID { get; set; }

        [Required]
        public bool AcceptTerms { get; set; }




    }

}