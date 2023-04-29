using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Site.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static Site.Models.Data;

namespace Site.Models.Page
{
    public partial class PageClient
    {
        public class PageRoutes
        {
            public Pages Page { get; set; }
            public PageTemplates PageTemplate { get; set; }
            public WebsiteLanguages WebsiteLanguage { get; set; }
            public Languages Language { get; set; }
            public string FullUrl { get; set; }
        }

        public class PageBundle
        {
            public Pages Page { get; set; }
            public PageTemplates PageTemplate { get; set; }
        }

        public class PageActive
        {
            public Pages Page { get; set; }
            public string Type { get; set; }
        }
    }
}