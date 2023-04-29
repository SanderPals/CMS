using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Site.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace Site.Models
{
    public class Website
    {
        private readonly SiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public Website(SiteContext context, UserManager<ApplicationUser> userManager = null, IHttpContextAccessor httpContextAccessor = null, SignInManager<ApplicationUser> signInManager = null)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
        }

        public class WebsiteResourcesBundle
        {
            public WebsiteFields WebsiteField { get; set; }
            public WebsiteResources WebsiteResource { get; set; }
        }

        public class WebsiteBundle
        {
            public IEnumerable<Languages> Languages { get; set; }
            public IEnumerable<WebsiteFields> WebsiteFields { get; set; }
            public IEnumerable<WebsiteFiles> WebsiteFiles { get; set; }
            public WebsiteLanguages WebsiteLanguage { get; set; }
            public IEnumerable<WebsiteLanguages> WebsiteLanguages { get; set; }
            public IEnumerable<WebsiteResources> WebsiteResources { get; set; }
            public Websites Website { get; set; }
            public IEnumerable<WebsiteUploads> WebsiteUploads { get; set; }
        }

        public Websites GetWebsiteById(int id)
        {
            return _context.Websites.FirstOrDefault(Websites => Websites.Id == id);
        }

        public IEnumerable<WebsiteBundle> GetWebsiteBundlesByCompanyId(int CompanyId)
        {
            return _context.Websites.GroupJoin(_context.WebsiteLanguages
                                               .Join(_context.Languages, WebsiteLanguages => WebsiteLanguages.LanguageId, Languages => Languages.Id, (WebsiteLanguages, Languages) => new { WebsiteLanguages, Languages })
                                               , Websites => Websites.Id, x => x.WebsiteLanguages.WebsiteId, (Websites, WebsiteLanguages) => new { Websites, WebsiteLanguages })
                                    .GroupJoin(_context.WebsiteFields, x => x.Websites.Id, WebsiteFields => WebsiteFields.WebsiteId, (x, WebsiteFields) => new { x.Websites, x.WebsiteLanguages, WebsiteFields })
                                    .GroupJoin(_context.WebsiteUploads, x => x.Websites.Id, WebsiteUploads => WebsiteUploads.WebsiteId, (x, WebsiteUploads) => new { x.Websites, x.WebsiteLanguages, x.WebsiteFields, WebsiteUploads })
                                    .Where(x => x.Websites.CompanyId == CompanyId)
                                    .Select(x => new WebsiteBundle()
                                    {
                                        Languages = x.WebsiteLanguages.Select(y => y.Languages),
                                        WebsiteFields = x.WebsiteFields,
                                        WebsiteFiles = null,
                                        WebsiteLanguage = null,
                                        WebsiteLanguages = x.WebsiteLanguages.Select(y => y.WebsiteLanguages),
                                        WebsiteResources = null,
                                        Website = x.Websites,
                                        WebsiteUploads = x.WebsiteUploads
                                    });
        }

        public IEnumerable<WebsiteBundle> GetWebsitesByCompanyId(int CompanyId)
        {
            IEnumerable<WebsiteBundle> WebsiteBundles = null;

            WebsiteBundles = _context.Websites.GroupJoin(_context.WebsiteLanguages.Join(_context.Languages, WebsiteLanguages => WebsiteLanguages.LanguageId, Languages => Languages.Id, (WebsiteLanguages, Languages) => new { WebsiteLanguages, Languages }), Websites => Websites.Id, x => x.WebsiteLanguages.WebsiteId, (Websites, WebsiteLanguages) => new { Websites, WebsiteLanguages })
                                              .Where(x => x.Websites.CompanyId == CompanyId)
                                              .Select(x => new WebsiteBundle()
                                              {
                                                  Languages = x.WebsiteLanguages.Select(y => y.Languages),
                                                  WebsiteFields = null,
                                                  WebsiteFiles = null,
                                                  WebsiteLanguage = null,
                                                  WebsiteLanguages = x.WebsiteLanguages.Select(y => y.WebsiteLanguages),
                                                  WebsiteResources = null,
                                                  Website = x.Websites,
                                                  WebsiteUploads = null
                                              }).ToList();

            return WebsiteBundles;
        }

        public Websites GetWebsiteByWebsiteLanguageId(int websiteLanguageId)
        {
            return _context.WebsiteLanguages.Join(_context.Websites, WebsiteLanguages => WebsiteLanguages.WebsiteId, Websites => Websites.Id, (WebsiteLanguages, Websites) => new { WebsiteLanguages, Websites })
                                            .Where(x => x.WebsiteLanguages.Id == websiteLanguageId)
                                            .Select(x => x.Websites)
                                            .FirstOrDefault();
        }

        public IEnumerable<WebsiteBundle> GetWebsiteBundlesByUserId(string UserId)
        {
            IEnumerable<WebsiteBundle> _websiteBundles = null;

            _websiteBundles = _context.Websites.GroupJoin(_context.WebsiteLanguages.Join(_context.Languages, WebsiteLanguages => WebsiteLanguages.LanguageId, Languages => Languages.Id, (WebsiteLanguages, Languages) => new { WebsiteLanguages, Languages }), Websites => Websites.Id, x => x.WebsiteLanguages.WebsiteId, (Websites, WebsiteLanguages) => new { Websites, WebsiteLanguages })
                                               .Join(_context.Companies, x => x.Websites.CompanyId, Companies => Companies.Id, (x, Companies) => new { x.Websites, x.WebsiteLanguages, Companies })
                                               .Join(_context.CompanyUsers, x => x.Companies.Id, CompanyUsers => CompanyUsers.CompanyId, (x, CompanyUsers) => new { x.Websites, x.WebsiteLanguages, x.Companies, CompanyUsers })
                                               .Where(x => x.CompanyUsers.UserId == UserId)
                                               .Select(x => new WebsiteBundle()
                                               {
                                                   Languages = x.WebsiteLanguages.Select(y => y.Languages),
                                                   WebsiteFields = null,
                                                   WebsiteFiles = null,
                                                   WebsiteLanguage = null,
                                                   WebsiteLanguages = x.WebsiteLanguages.Select(y => y.WebsiteLanguages),
                                                   WebsiteResources = null,
                                                   Website = x.Websites,
                                                   WebsiteUploads = null
                                               });
            
            return _websiteBundles;
        }

        public string GetWebsiteUrl(int websiteLanguageId)
        {
            Websites Website = GetWebsiteByWebsiteLanguageId(websiteLanguageId);

            string Subdomain = "";
            if (Website.Subdomain != "")
            {
                Subdomain = Website.Subdomain + ".";
            }

            return Website.TypeClient + "://" + Subdomain + Website.Domain + "." + Website.Extension;
        }

        public string GetCleanWebsiteUrlByWebsiteId(int websiteId)
        {
            Websites _website = _context.Websites.FirstOrDefault(Website => Website.Id == websiteId);

            string Subdomain = "";
            if (_website.Subdomain != "")
            {
                Subdomain = _website.Subdomain + ".";
            }

            return Subdomain + _website.Domain + "." + _website.Extension;
        }

        public async Task<bool> SetWebsiteLanguageIdClaimAsync(string websiteLanguageId, string userId)
        {
            User _user = new User(_context, _userManager, _httpContextAccessor);
            string websiteId = _user.GetClaimByType("WebsiteId").Value;

            WebsiteLanguages _websiteLanguage = GetWebsiteBundlesByUserId(userId).FirstOrDefault(x => x.Website.Id == Int32.Parse(websiteId)).WebsiteLanguages.FirstOrDefault(WebsiteLanguages => WebsiteLanguages.Id == Int32.Parse(websiteLanguageId));
            if (_websiteLanguage != null)
            {
                await _user.RemoveClaimByTypeAsync("WebsiteLanguageId", userId);
                await _userManager.AddClaimAsync(await _userManager.FindByIdAsync(userId), new Claim("WebsiteLanguageId", _websiteLanguage.Id.ToString()));

                //Reset user cookie
                await _signInManager.RefreshSignInAsync(await _userManager.FindByIdAsync(userId));
            }

            return true;
        }

        public async Task<bool> SetWebsiteClaimsByWebsiteIdAsync(string websiteId, string userId)
        {
            WebsiteBundle _websiteBundle = GetWebsiteBundlesByUserId(userId).FirstOrDefault(x => x.Website.Id == Int32.Parse(websiteId));
            if (_websiteBundle != null)
            {
                User _user = new User(_context, _userManager, _httpContextAccessor);
                await _user.RemoveClaimByTypeAsync("WebsiteId", userId);
                await _user.RemoveClaimByTypeAsync("WebsiteLanguageId", userId);

                await _userManager.AddClaimAsync(await _userManager.FindByIdAsync(userId), new Claim("WebsiteId", websiteId));

                WebsiteLanguages _websiteLanguage = _websiteBundle.WebsiteLanguages.FirstOrDefault(WebsiteLanguages => WebsiteLanguages.DefaultLanguage == true);
                string WebsiteLanguageId;
                if (_websiteLanguage != null)
                {
                    WebsiteLanguageId = _websiteLanguage.Id.ToString();
                }
                else
                {
                    WebsiteLanguageId = _websiteBundle.WebsiteLanguages.FirstOrDefault(x => x.DefaultLanguage == false).Id.ToString();
                }

                await _userManager.AddClaimAsync(await _userManager.FindByIdAsync(userId), new Claim("WebsiteLanguageId", WebsiteLanguageId));

                //Reset user cookie
                await _signInManager.RefreshSignInAsync(await _userManager.FindByIdAsync(userId));
            }

            return true;
        }

        public async Task<bool> SetWebsiteClaimsByUserIdAsync(string userId)
        {
            User _user = new User(_context, _userManager, _httpContextAccessor);

            Claim websiteIdClaim = _user.GetClaimByType("WebsiteId");
            Claim websiteLanguageIdClaim = _user.GetClaimByType("WebsiteLanguageId");
            if (websiteIdClaim == null || websiteLanguageIdClaim == null)
            {
                await _user.RemoveClaimByTypeAsync("WebsiteId", userId);
                await _user.RemoveClaimByTypeAsync("WebsiteLanguageId", userId);

                IEnumerable<WebsiteBundle> WebsiteBundles = GetWebsiteBundlesByUserId(userId);
                if (WebsiteBundles != null)
                {
                    string websiteId = WebsiteBundles.FirstOrDefault().Website.Id.ToString();
                    IdentityResult claim = await _userManager.AddClaimAsync(await _userManager.FindByIdAsync(userId), new Claim("WebsiteId", websiteId));

                    string WebsiteLanguageId;
                    WebsiteLanguages _websiteLanguage = WebsiteBundles.FirstOrDefault().WebsiteLanguages.FirstOrDefault(x => x.DefaultLanguage == true);
                    if (_websiteLanguage != null)
                    {
                        WebsiteLanguageId = _websiteLanguage.Id.ToString();
                    }
                    else
                    {
                        WebsiteLanguageId = WebsiteBundles.FirstOrDefault().WebsiteLanguages.FirstOrDefault(x => x.DefaultLanguage == false).Id.ToString();
                    }
                    await _userManager.AddClaimAsync(await _userManager.FindByIdAsync(userId), new Claim("WebsiteLanguageId", WebsiteLanguageId));

                    //Reset user cookie
                    await _signInManager.RefreshSignInAsync(await _userManager.FindByIdAsync(userId));
                }
            }

            return true;
        }

        //public bool SetWebsiteSessionsByUserId(string UserId)
        //{
        //    WebsiteBundle WebsiteBundle = GetWebsiteByUserId(UserId);

        //    if (WebsiteBundle != null)
        //    {
        //        _session.Remove("WebsiteId");
        //        _session.SetInt32("WebsiteId", WebsiteBundle.Website.Id);

        //        var DefaultLanguage = WebsiteBundle.WebsiteLanguages.FirstOrDefault(x => x.DefaultLanguage == true);
        //        var WebsiteLanguageId = 0;
        //        if (DefaultLanguage != null)
        //        {
        //            WebsiteLanguageId = DefaultLanguage.Id;
        //        }
        //        else
        //        {
        //            WebsiteLanguageId = WebsiteBundle.WebsiteLanguages.FirstOrDefault(x => x.DefaultLanguage == false).Id;
        //        }
        //        _session.Remove("WebsiteLanguageId");
        //        _session.SetInt32("WebsiteLanguageId", WebsiteLanguageId);
        //    }

        //    return true;
        //}
    }
}
