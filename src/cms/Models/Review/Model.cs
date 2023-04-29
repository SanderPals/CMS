using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Site.Data;

namespace Site.Models.Review
{
    public partial class ReviewClient
    {
        public class ReviewBundle
        {
            public Reviews Review { get; set; }
            public ReviewTemplates ReviewTemplate { get; set; }
        }
    }
}
