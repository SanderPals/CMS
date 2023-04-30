using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Models;
using Site.Models.Setting;
using System;
using Microsoft.Extensions.Localization;

namespace Site.Controllers
{
    public class SettingController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<SettingController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public SettingController(SiteContext context,
            IStringLocalizer<SettingController> localizer, UserManager<ApplicationUser> userManager = null, IHostingEnvironment env = null, IHttpContextAccessor httpContextAccessor = null, IHttpContextAccessor contextAccessor = null)
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

        [Route("/api/commerce-settings")]
        [HttpGet]
        public IActionResult GetSettingsApi()
        {
            try
            {
                return new SettingClient(_context).GetCommerceSettings(websiteId);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotGetCommerceSettings"].Value
                }));
            }
        }

        [Route("/api/setting")]
        [HttpPost]
        public IActionResult UpdateSettingApi(Settings setting)
        {
            try
            {
                return new SettingClient(_context).ValidateAndUpdateSetting(setting, websiteId);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateSetting"].Value
                }));
            }
        }

        [Route("/commerce-settings")]
        public IActionResult Commerce()
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { LocalRedirect("/dashboard"); }

            return View();
        }
    }
}