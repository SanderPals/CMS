using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Site.Models.Account.ViewModels
{
    public class SendCodeViewModel
    {
        public string Email { get; set; }

        public string SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }

        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }
}
