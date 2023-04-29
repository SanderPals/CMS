using System.ComponentModel.DataAnnotations;

namespace Site.Models.Account.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "The email address field is required.")]
        [EmailAddress(ErrorMessage = "This is not a valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The password field is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
