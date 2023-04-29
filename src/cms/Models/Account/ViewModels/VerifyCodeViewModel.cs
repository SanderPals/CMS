using System.ComponentModel.DataAnnotations;

namespace Site.Models.Account.ViewModels
{
    public class VerifyCodeViewModel
    {
        [Required(ErrorMessage = "The provider field is required.")]
        public string Provider { get; set; }

        [Required(ErrorMessage = "The code field is required.")]
        public string Code { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
