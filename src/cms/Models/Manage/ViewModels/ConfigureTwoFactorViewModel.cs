using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Site.Models.Manage.ViewModels
{
    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }
    }
}
