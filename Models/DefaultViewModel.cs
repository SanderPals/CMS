using Site.Data;
using System.Collections.Generic;
using static Site.Models.Website;

namespace Site.Models
{
    public class DefaultViewModel
    {
        public IEnumerable<DataTemplates> DataTemplates { get; set; }
        public IEnumerable<Navigations> Navigations { get; set; }
        public IEnumerable<ReviewTemplates> ReviewTemplates { get; set; }
        public IEnumerable<WebsiteBundle> WebsiteBundles { get; set; }
        public IEnumerable<LanguageTranslate> LanguageTranslates { get; set; }
        public string Ecommerce { get; set; }
    }
}   