using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Site.Data;

namespace Site.Models.Review
{
    public partial class ReviewClient : Controller
    {
        SiteContext _context;

        public ReviewClient(SiteContext context)
        {
            _context = context;
        }
    }
}
