using System.ComponentModel.DataAnnotations;

namespace Site.Models.Account.ViewModels
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
