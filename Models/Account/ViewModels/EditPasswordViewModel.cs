using System.ComponentModel.DataAnnotations;

namespace Site.Models.Account.ViewModels
{
    public class EditPasswordViewModel
    {
        [Required(ErrorMessage = "The current password field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "current password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "The new password field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "new password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "The confirm new password field is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
