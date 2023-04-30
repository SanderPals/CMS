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
        SiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;

        public PageClient(SiteContext context, UserManager<ApplicationUser> userManager = null, IHttpContextAccessor httpContextAccessor = null)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public IEnumerable<Pages> GetPagesByParent(int parent)
        {
            return _context.Pages.Where(Pages => Pages.Parent == parent);
        }

        public IQueryable<PageBundle> GetPagesAndPageTemplatesByWebsiteLanguageIdAndOrderByTitle(int websiteLanguageId)
        {
            return _context.Pages.Join(_context.PageTemplates, Pages => Pages.PageTemplateId, PageTemplates => PageTemplates.Id, (Pages, PageTemplates) => new { Pages, PageTemplates })
                                 .Where(x => x.Pages.WebsiteLanguageId == websiteLanguageId)
                                 .OrderBy(x => x.Pages.Title)
                                 .Select(x => new PageBundle()
                                 {
                                     Page = x.Pages,
                                     PageTemplate = x.PageTemplates
                                 });
        }

        public IQueryable<Pages> GetPagesByWebsiteLanguageIdAndTypeAndOrderByTitle(int websiteLanguageId, string type)
        {
            return _context.Pages.Join(_context.PageTemplates, Pages => Pages.PageTemplateId, PageTemplates => PageTemplates.Id, (Pages, PageTemplates) => new { Pages, PageTemplates })
                                 .Where(x => x.PageTemplates.Type == type)
                                 .Select(x => x.Pages)
                                 .Where(Pages => Pages.WebsiteLanguageId == websiteLanguageId)
                                 .OrderBy(Pages => Pages.Name);
        }

        public IQueryable<Pages> GetPagesByWebsiteLanguageIdAndActiveAndOrderByTitle(int websiteLanguageId, bool active)
        {
            return _context.Pages.Where(Pages => Pages.WebsiteLanguageId == websiteLanguageId && Pages.Active == active).OrderBy(Pages => Pages.Title);
        }

        public IQueryable<Pages> GetPagesByWebsiteLanguageIdAndActiveAndTypeAndNameAndOrderByTitle(int websiteLanguageId, bool active, string type, string name)
        {
            return _context.Pages.Join(_context.PageTemplates, Pages => Pages.PageTemplateId, PageTemplates => PageTemplates.Id, (Pages, PageTemplates) => new { Pages, PageTemplates })
                                 .Where(x => x.PageTemplates.Type == type)
                                 .Select(x => x.Pages)
                                 .Where(Pages => Pages.WebsiteLanguageId == websiteLanguageId && Pages.Active == active && Pages.Name.ToLower().Contains(name.ToLower()))
                                 .OrderBy(Pages => Pages.Title);
        }

        public IQueryable<PageTemplateSections> GetPageTemplateSectionsByDataItemAlternateGuid(string alternateGuid, int websiteLanguageId) {
            return _context.Pages.Where(Pages => Pages.AlternateGuid == alternateGuid && Pages.WebsiteLanguageId == websiteLanguageId)
                                 .Join(_context.PageTemplateSections, Pages => Pages.PageTemplateId, PageTemplateSections => PageTemplateSections.PageTemplateId, (Pages, PageTemplateSections) => new { Pages, PageTemplateSections })
                                 .Select(x => x.PageTemplateSections);
        }

        public Pages GetPageByAlternateGuidAndWebsiteLanguageId(int websitelanguageId, string alternateGuid) {
            return _context.Pages.FirstOrDefault(Pages => Pages.WebsiteLanguageId == websitelanguageId && Pages.AlternateGuid == alternateGuid);
        }

        public PageTemplateSections GetPageTemplateSectionByPageTemplateIdAndId(int pageTemplateId, int id)
        {
            return _context.PageTemplateSections.FirstOrDefault(PageTemplateSections => PageTemplateSections.PageTemplateId == pageTemplateId && PageTemplateSections.Id == id);
        }

        public Pages GetPageByWebsiteLanguageIdAndNameAndTypeNotByPageId(int websiteLanguageId, string name, string type, int id)
        {
            return _context.Pages.Where(Pages => Pages.WebsiteLanguageId == websiteLanguageId && Pages.Id != id && Pages.Name == name)
                                 .Join(_context.PageTemplates, Pages => Pages.PageTemplateId, PageTemplates => PageTemplates.Id, (Pages, PageTemplates) => new { Pages, PageTemplates })
                                 .Where(x => x.PageTemplates.Type == type)
                                 .Select(x => x.Pages)
                                 .FirstOrDefault();
        }

        public Pages GetPageByIdAndType(int id, string type)
        {
            return _context.Pages.Join(_context.PageTemplates, Pages => Pages.PageTemplateId, PageTemplates => PageTemplates.Id, (Pages, PageTemplates) => new { Pages, PageTemplates })
                                 .Where(x => x.PageTemplates.Type == type)
                                 .Select(x => x.Pages)
                                 .FirstOrDefault(Pages => Pages.Id == id);
        }

        public string GetPageAlternateGuidById(int id)
        {
            Pages _page = _context.Pages.FirstOrDefault(Pages => Pages.Id == id);

            return (_page != null) ? _page.AlternateGuid : "";
        }

        public bool CheckIfRootPageById(int id)
        {
            Pages _page = _context.Pages.Join(_context.Websites, Pages => Pages.AlternateGuid, Websites => Websites.RootPageAlternateGuid, (Pages, Websites) => new { Pages, Websites })
                                        .Select(x => x.Pages)
                                        .FirstOrDefault(Pages => Pages.Id == id);

            return (_page != null) ? true : false;
        }

        public bool PageUrlValidation(string url)
        {
            List<string> invalidWords = new List<string>() {
                "spine-content", "spine-auth", "spine-api", "spine-service", "spine-callback", "api",
                "nl", "nl-be", "nl-nl",
                "en", "en-au ", "en-bz", "en-ca", "en-cb", "en-ie", "en-jm", "en-ph", "en-za", "en-tt", "en-gb", "en-us", "en-zw",
                "de", "de-at ", "de-de", "de-li", "de-lu", "de-ch",
                "fr", "fr-be ", "fr-ca", "fr-fr", "fr-lu", "fr-mc", "fr-ch"
            };

            return (invalidWords.Contains(url)) ? false : true;
        }

        public string ValidateNewAlternateGuid(string alternateGuid)
        {
            Pages _page = _context.Pages.FirstOrDefault(Pages => Pages.AlternateGuid == alternateGuid);
            if (_page != null)
            {
                return ValidateNewAlternateGuid(Guid.NewGuid().ToString());
            }
            else
            {
                return alternateGuid;
            }
        }

        public bool DeletePageById(int id)
        {
            _context.Pages.Remove(_context.Pages.FirstOrDefault(Pages => Pages.Id == id));
            _context.SaveChanges();

            return true;
        }

        public bool UpdatePageActive(int id, bool active)
        {
            Pages _page = new Pages { Id = id, Active = active };
            _context.Pages.Attach(_page);
            _context.Entry(_page).Property(r => r.Active).IsModified = true;
            _context.SaveChanges();

            return true;
        }

        public List<Dictionary<string, object>> ConvertPagesAndPageTemplatesToJson(IQueryable<PageBundle> pageBundles, int websiteId)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();

            string rootPageAlternateGuid = _context.Websites.FirstOrDefault(Website => Website.Id == websiteId).RootPageAlternateGuid.ToString();

            foreach (PageBundle pageBundle in pageBundles)
            {
                Dictionary<string, object> childRow = new Dictionary<string, object>
                {
                    {"id", pageBundle.Page.Id},
                    {"parent", pageBundle.Page.Parent},
                    {"name", pageBundle.Page.Name},
                    {"alternateGuid", pageBundle.Page.AlternateGuid},
                    {"type", (pageBundle.PageTemplate.Type.ToLower() == "page") ? "page" : "E-commerce category"}
                };

                if (pageBundle.Page.AlternateGuid == rootPageAlternateGuid)
                {
                    childRow.Add("rootPage", true);
                }
                else
                {
                    childRow.Add("rootPage", false);
                }

                parentRow.Add(childRow);
            }

            return parentRow;
        }

        public List<Dictionary<string, object>> ConvertPagesToJson(IQueryable<Pages> pages)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            foreach (Pages page in pages)
            {
                Dictionary<string, object>  childRow = new Dictionary<string, object>
                {
                    {"id", page.Id},
                    {"name", page.Name},
                    {"alternateGuid", page.AlternateGuid},
                };

                parentRow.Add(childRow);
            }

            return parentRow;
        }

        public Dictionary<string, object> ConvertPageOptionsToJson(IQueryable<Pages> pages)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();

            foreach (Pages page in pages)
            {
                Dictionary<string, object> childRow = new Dictionary<string, object>
                {
                    {"id", page.AlternateGuid},
                    {"text", page.Name}
                };

                parentRow.Add(childRow);
            }


            return new Dictionary<string, object> {
                { "results", parentRow },
                { "pagination", new Dictionary<string, object> { { "more", false } } }
            };
        }

        public List<Dictionary<string, object>> CreatePageOptionsArrayList(IQueryable<Pages> pages)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();

            foreach (Pages page in pages)
            {
                Dictionary<string, object> childRow = new Dictionary<string, object>
                {
                    {"id", page.AlternateGuid},
                    {"text", page.Name}
                };

                parentRow.Add(childRow);
            }

            return parentRow;
        }

        public List<Dictionary<string, object>> CreatePageTemplateSectionOptionsArrayList(IQueryable<PageTemplateSections> pageTemplateSections)
        {
            List<Dictionary<string, object>> sectionsParentRow = new List<Dictionary<string, object>>();
            foreach (PageTemplateSections pageTemplateSection in pageTemplateSections.Where(PageTemplateSections => PageTemplateSections.Type.ToLower() == "section"))
            {
                Dictionary<string, object> sectionsChildRow = new Dictionary<string, object>
                {
                    {"id", pageTemplateSection.Id},
                    {"text", pageTemplateSection.Name}
                };
                sectionsParentRow.Add(sectionsChildRow);
            }

            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            if (sectionsParentRow.Count() != 0)
            {
                Dictionary<string, object> childRow = new Dictionary<string, object>
                {
                    {"text", "Sections"},
                    {"children", sectionsParentRow}
                };
                parentRow.Add(childRow);
            }

            foreach (PageTemplateSections pageTemplateSection in pageTemplateSections.Where(PageTemplateSections => PageTemplateSections.Type.ToLower() == "dataFilter"))
            {
                IQueryable<DataBundle> _dataBundles = new Data(_context).GetDataTemplateAndDataItemsByDataTemplateId(pageTemplateSection.LinkedToDataTemplateId);
                foreach (DataBundle dataBundle in _dataBundles)
                {
                    List<Dictionary<string, object>> dataItemsParentRow = new List<Dictionary<string, object>>();
                    foreach (DataItems dataItem in dataBundle.DataItems)
                    {
                        Dictionary<string, object> dataItemsChildRow = new Dictionary<string, object>
                        {
                            {"id", pageTemplateSection.Id + ":" + dataItem.AlternateGuid},
                            {"text", dataItem.Title}
                        };

                        dataItemsParentRow.Add(dataItemsChildRow);
                    }

                    Dictionary<string, object> childRow = new Dictionary<string, object>
                    {
                        {"text", "Filters " + dataBundle.DataTemplate.Name.ToLower()},
                        {"children", dataItemsParentRow}
                    };

                    parentRow.Add(childRow);
                }
            }

            return parentRow;
        }

        public List<Dictionary<string, object>> GetPageOptionsArrayList(int websiteLanguageId)
        {
            return CreatePageOptionsArrayList(GetPagesByWebsiteLanguageIdAndActiveAndOrderByTitle(websiteLanguageId, true));
        }

        public Dictionary<string, object> GetPageOptionsArrayList(int websiteLanguageId, string type, string name)
        {
            return ConvertPageOptionsToJson(GetPagesByWebsiteLanguageIdAndActiveAndTypeAndNameAndOrderByTitle(websiteLanguageId, true, type, name));
        }

        public List<Dictionary<string, object>> GetSectionOptionsArrayList(string alternateGuid, int websiteLanguageId)
        {
            IQueryable<PageTemplateSections> _pageTemplateSections = GetPageTemplateSectionsByDataItemAlternateGuid(alternateGuid, websiteLanguageId);
            return CreatePageTemplateSectionOptionsArrayList(_pageTemplateSections);
        }
    }
}