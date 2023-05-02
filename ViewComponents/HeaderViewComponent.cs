using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Site.Data;
using Site.Models;
using Site.Models.Company;
using Site.Models.Review;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using static Site.Models.Website;
using static Site.Startup;

namespace Site.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly SiteContext _context;
        private readonly IOptions<MyConfig> _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;

        int websiteId;
        int websiteLanguageId;

        public HeaderViewComponent(SiteContext context, IOptions<MyConfig> config, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IHttpContextAccessor httpContextAccessor, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _config = config;
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _contextAccessor = contextAccessor;

            websiteId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId").Value);
            websiteLanguageId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteLanguageId").Value);
        }

        public IViewComponentResult Invoke(ActionExecutingContext actionContext)
        {
            ViewBag.WebsiteId = websiteId;
            ViewBag.WebsiteLanguageId = websiteLanguageId;

            IEnumerable<ReviewTemplates> _reviewTemplates = new ReviewClient(_context).GetReviewTemplatesByWebsiteId(websiteId);

            var UserId = _userManager.GetUserId(HttpContext.User);
            Companies Company = new Company(_context).GetCompanyByUserId(UserId);

            IEnumerable<WebsiteBundle> WebsiteBundles = null;
            WebsiteBundles = new Website(_context, _userManager, _httpContextAccessor).GetWebsiteBundlesByCompanyId(Company.Id);
            IEnumerable<LanguageTranslate> _languageTranslates = _context.LanguageTranslate.ToList();


            IEnumerable<DataTemplates> _dateTemplates = new Models.Data(_context).GetDataTemplatesByMenuTypeAndWebsiteId("main", websiteId);

            IEnumerable<Navigations> _navigations = new Navigation(_context).GetNavigationsByWebsiteId(websiteId);

            DefaultViewModel DefaultViewModel = new DefaultViewModel()
            {
                DataTemplates = _dateTemplates,
                Navigations = _navigations,
                ReviewTemplates = _reviewTemplates,
                WebsiteBundles = WebsiteBundles,
                LanguageTranslates = _languageTranslates,
                Ecommerce = new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId)
            };

            return View("_Header", DefaultViewModel);
        }
    }
}
