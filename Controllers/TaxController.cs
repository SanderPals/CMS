using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Models;
using System;
using Microsoft.Extensions.Localization;

namespace Site.Controllers
{
    public class TaxController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<TaxController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public TaxController(SiteContext context,
            IStringLocalizer<TaxController> localizer, UserManager<ApplicationUser> userManager = null, IHostingEnvironment env = null, IHttpContextAccessor httpContextAccessor = null, IHttpContextAccessor contextAccessor = null)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _contextAccessor = contextAccessor;
            _localizer = localizer;

            websiteId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId").Value);
            websiteLanguageId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteLanguageId").Value);
        }

        [Route("/spine-api/tax-classes")]
        [HttpGet]
        public IActionResult GetShippingClassesApi()
        {
            try
            {
                return Ok(Json(new Tax(_context).GetTaxClassesByWebsiteId(websiteId)));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowListItems"].Value
                }));
            }
        }

        //[Route("/products")]
        //public IActionResult Index()
        //{
        //    return View();
        //}

        //[Route("/products/add")]
        //[Route("/products/edit/{id}")]
        //public IActionResult Add()
        //{
        //    return View();
        //}
    }
}