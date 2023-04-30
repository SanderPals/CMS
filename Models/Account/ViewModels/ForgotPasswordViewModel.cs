using System.ComponentModel.DataAnnotations;

namespace Site.Models.Account.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
