using Site.Data;
using System.Collections.Generic;

namespace Site.Models.Page.ViewModels
{
    public class PageAddViewModel
    {
        public string Url { get; set; }
        public IEnumerable<PageTemplates> PageTemplates { get; set; }
    }
}