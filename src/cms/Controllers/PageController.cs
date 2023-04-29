using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Site.Data;
using Site.Models;
using Site.Models.Page;
using Site.Models.Page.ViewModels;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Localization;
using System.Threading.Tasks;
using static Site.Models.Page.PageClient;

namespace Site.Controllers
{
    public class PageController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<PageController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public PageController(SiteContext context, UserManager<ApplicationUser> userManager, IHostingEnvironment env, IHttpContextAccessor httpContextAccessor, IHttpContextAccessor contextAccessor,
            IStringLocalizer<PageController> localizer)
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

        private string CheckAvailibiltyFile(string Location, string LocationNew, int Count)
        {
            if (System.IO.File.Exists(LocationNew))
            {
                string fDir = Path.GetDirectoryName(Location);
                string fName = Path.GetFileNameWithoutExtension(Location);
                string fExt = Path.GetExtension(Location);
                LocationNew = Path.Combine(fDir, string.Concat(fName, "_" + Count++, fExt));

                LocationNew = CheckAvailibiltyFile(Location, LocationNew, Count);
            }

            return LocationNew;
        }

        private string ValidateFileName(string FileName)
        {
            FileName = FileName.Trim(Path.GetInvalidFileNameChars());

            return FileName;
        }

        [Route("/spine-api/page/delete")]
        [HttpPost]
        public IActionResult DeletePage(int id, string type)
        {
            try
            {
                PageClient page = new PageClient(_context);

                //Check if this is the root page. If so, then don't delete it and give the user a message.
                bool rootPage = page.CheckIfRootPageById(id);
                if (rootPage)
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["CannotDeleteRootPage"].Value
                    }));
                }

                //Check if this page has childs. If so, then don't delete it and give the user a message.
                IEnumerable<Pages> _pages = page.GetPagesByParent(id);
                if (_pages.Count() != 0)
                {
                    string msg = (type != "page") 
                        ? _localizer["ThisCategoryHasChilds"].Value
                        : _localizer["ThisPageHasChilds"].Value;
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = msg
                    }));
                }

                string alternateGuid = page.GetPageAlternateGuidById(id);

                //Check if the page is in a navigation
                IEnumerable<Navigations> _navigations = new Navigation(_context).GetNavigationsByLinkedToAlternateGuidAndWebsiteId(alternateGuid, "page", websiteId);
                if (_navigations.Count() != 0)
                {
                    string navigationsString = string.Join("<br />- ", _navigations.GroupBy(Navigations => Navigations.Id).Select(x => x.FirstOrDefault().Name));
                    string msg = (type != "page") 
                        ? _localizer["ThisCategoryIsInFollowingNavs", navigationsString].Value
                        : _localizer["ThisPageIsInFollowingNavs", navigationsString].Value;
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = msg
                    }));
                }

                //Check if this page has projects, blogs or something else linked. If so, then don't delete it and give the user a message.
                DataTemplates _data = new Site.Models.Data(_context).GetDataTemplateByPageAlternateGuid(alternateGuid);
                if (_data != null)
                {
                    string msg = (type != "page") 
                        ? _localizer["ThisCategoryHasLinked", _data.Name].Value
                        : _localizer["ThisPageHasLinked", _data.Name].Value;
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = msg
                    }));
                }

                page.DeletePageById(id);

                Websites _website = new Website(_context).GetWebsiteById(websiteId);

                var path = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\pages\{id}";
                if (Directory.Exists(path))
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    Directory.Delete(path);
                }

                path = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\pages\{id}";
                if (Directory.Exists(path))
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    Directory.Delete(path);
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                string msg = (type != "page") 
                    ? _localizer["CouldNotDeleteCategory"].Value
                    : _localizer["CouldNotDeletePage"].Value;
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = msg
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = (type != "page") 
                ? _localizer["CategoryIsDeleted"].Value
                : _localizer["PageIsDeleted"].Value
            }));
        }

        [Route("/spine-api/page/active")]
        [HttpPost]
        public IActionResult UpdateActive([FromBody]PageActive pageActive)
        {
            try
            {
                PageClient page = new PageClient(_context);

                //Check if this is the root page. If so, then don't delete it and give the user a message.
                bool rootPage = page.CheckIfRootPageById(pageActive.Page.Id);
                if (rootPage)
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["CannotDeActivateRootPage"].Value
                    }));
                }


                page.UpdatePageActive(pageActive.Page.Id, pageActive.Page.Active ?? true);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateStatus"].Value
                }));
            }

            string type = (pageActive.Type != "page") ? _localizer["Category"].Value : _localizer["Page"].Value;
            string message = _localizer["IsUnPublished"].Value;
            if (pageActive.Page.Active ?? true)
            {
                message = _localizer["IsPublished"].Value;
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = message
            }));
        }

        [Route("/spine-api/pages")]
        [HttpGet]
        public IActionResult GetList()
        {
            string msg = _localizer["CannotShowPages"].Value;
            string msg2 = _localizer["CannotShowPagesAndCategories"].Value;

            try
            {
                msg = (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() == "true") ? msg2 : msg;

                PageClient page = new PageClient(_context);
                return Ok(Json(new
                {
                    data = page.ConvertPagesAndPageTemplatesToJson(page.GetPagesAndPageTemplatesByWebsiteLanguageIdAndOrderByTitle(websiteLanguageId), websiteId),
                    maxDepth = 7
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = msg
                }));
            }
        }

        [Route("/spine-api/pages-by-type")]
        [HttpGet]
        public IActionResult GetPagaesByTypeApi(string type)
        {
            string msg = _localizer["CannotShowPages"].Value;

            try
            {
                if (type == "eCommerceCategory")
                {
                    msg = _localizer["CannotShowCategories"].Value;
                }
                else
                {
                    type = "page";
                }

                PageClient page = new PageClient(_context);
                return Ok(Json(new
                {
                    data = page.ConvertPagesToJson(page.GetPagesByWebsiteLanguageIdAndTypeAndOrderByTitle(websiteLanguageId, type)),
                    maxDepth = 7
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = msg
                }));
            }
        }

        [Route("/spine-api/page-options")]
        [HttpGet]
        public IActionResult GetOptions(string type)
        {
            try
            {
                return Ok(Json(new PageClient(_context).GetPageOptionsArrayList(websiteLanguageId, type, "")));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = (type != "page") 
                    ? _localizer["CannotShowCategories"].Value
                    : _localizer["CannotShowPages"].Value
                }));
            }
        }

        [Route("/spine-api/categories-by-name")]
        [HttpGet]
        public IActionResult GetCategoriesApi(string term)
        {
            try
            {
                return Ok(Json(new
                {
                    data = new PageClient(_context).GetPageOptionsArrayList(websiteLanguageId, "eCommerceCategory", term)
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowCategories"].Value
                }));
            }
        }

        [Route("/spine-api/page/template/sections/get/options")]
        [HttpGet]
        public IActionResult GetSectionOptions(string alternateGuid)
        {
            try
            {
                return Ok(Json(new
                {
                    data = new PageClient(_context).GetSectionOptionsArrayList(alternateGuid, websiteLanguageId)
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowSectionsAndFilters"].Value
                }));
            }
        }
        
        [Route("/spine-api/pages/update")]
        [HttpPost]
        public IActionResult UpdateList([FromBody] List<Pages> Pages)
        {
            foreach (var i in Pages)
            {
                var _page = new Pages { Id = i.Id, Parent = i.Parent };
                _context.Pages.Attach(_page);
                _context.Entry(_page).Property(p => p.Parent).IsModified = true;
            }

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CannotSaveBreadcrumbs"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                messageType = "success",
                message = _localizer["BreadcrumbsSaved"].Value
            });
        }

        [Route("/spine-api/page/get")]
        [HttpPost]
        public IActionResult Get(int Id, Pages Page, string type)
        {
            Pages _page = new PageClient(_context).GetPageByIdAndType(Id, type);

            if (_page == null)
            {
                string redirect = (type != "page") ? "/categories" : "/pages";
                return StatusCode(400, Json(new
                {
                    redirect = redirect
                }));
            }
            else
            {
                var _pageResources = _context.PageResources.Join(_context.PageTemplateFields, PageResources => PageResources.PageTemplateFieldId, PageTemplateFields => PageTemplateFields.Id, (PageResources, PageTemplateFields) => new { PageResources, PageTemplateFields }).Where(x => x.PageResources.PageId == _page.Id).OrderBy(x => x.PageTemplateFields.CustomOrder).ToList();

                List<Dictionary<string, object>> ParentRow = new List<Dictionary<string, object>>();
                Dictionary<string, object> ChildRow;
                foreach (var i in _pageResources)
                {
                    ChildRow = new Dictionary<string, object>
                    {
                        { "Id", i.PageResources.Id},
                        { "PageId", i.PageResources.PageId},
                        { "PageTemplateFieldId", i.PageResources.PageTemplateFieldId},
                        { "Text", i.PageResources.Text},
                        { "Heading", i.PageTemplateFields.Heading},
                        { "Type", i.PageTemplateFields.Type}
                    };
                    ParentRow.Add(ChildRow);
                }

                var _pageTemplateUploads = _context.PageTemplateUploads.Where(PageTemplateUploads => PageTemplateUploads.PageTemplateId == _page.PageTemplateId).OrderBy(PageTemplateUploads => PageTemplateUploads.CustomOrder).ToList();

                List<Dictionary<string, object>> PtuParentRow = new List<Dictionary<string, object>>();
                Dictionary<string, object> PtuChildRow;
                foreach (PageTemplateUploads PageTemplateUpload in _pageTemplateUploads)
                {
                    PtuChildRow = new Dictionary<string, object>
                    {
                        { "Id", PageTemplateUpload.Id},
                        { "PageTemplateId", PageTemplateUpload.PageTemplateId},
                        { "CallName", PageTemplateUpload.CallName},
                        { "Heading", PageTemplateUpload.Heading},
                        { "MimeTypes", PageTemplateUpload.MimeTypes},
                        { "FileExtensions", PageTemplateUpload.FileExtensions},
                        { "MinFiles", PageTemplateUpload.MinFiles},
                        { "MaxFiles", PageTemplateUpload.MaxFiles},
                        { "MaxSize", PageTemplateUpload.MaxSize},
                        { "Width", PageTemplateUpload.Width},
                        { "Height", PageTemplateUpload.Height},
                        { "CustomOrder", PageTemplateUpload.CustomOrder }
                    };
                    PtuParentRow.Add(PtuChildRow);
                }

                Websites _website = _context.Websites.Where(Website => Website.Id == websiteId).FirstOrDefault(Website => Website.RootPageAlternateGuid == _page.AlternateGuid);
                bool rootPage = false;
                if (_website != null) { rootPage = true; }

                return Ok(Json(new
                {
                    success = "Valid",
                    websiteLanguageId = _page.WebsiteLanguageId,
                    pageTemplateId = _page.PageTemplateId,
                    parent = _page.Parent,
                    pageId = _page.Id,
                    url = _page.Url,
                    title = _page.Title,
                    keywords = _page.Keywords,
                    description = _page.Description,
                    alternateGuid = _page.AlternateGuid,
                    active = _page.Active,
                    name = _page.Name,
                    rootPage = rootPage,
                    data = ParentRow,
                    uploads = PtuParentRow
                }));
            }
        }

        [Route("/spine-api/page/add")]
        [HttpPost]
        public IActionResult Post(int pageTemplateId, int parent, string url, string title, string keywords, string description, string alternateGuid, string name, string type)
        {
            string msg = "";

            if (!ModelState.IsValid) { return BadRequest(ModelState); }
            if (url == null) { url = ""; }
            if (keywords == null) { keywords = ""; }
            if (description == null) { description = ""; }

            PageClient page = new PageClient(_context);
            if (alternateGuid == null)
            {
                alternateGuid = page.ValidateNewAlternateGuid(Guid.NewGuid().ToString());
            }

            Pages _page = new PageClient(_context).GetPageByWebsiteLanguageIdAndNameAndTypeNotByPageId(websiteLanguageId, name, type, 0);
            if (_page != null)
            {
                msg = (type != "page")
                        ? _localizer["CategoryNameAlreadyExist"].Value
                        : _localizer["PageNameAlreadyExist"].Value;
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = msg
                }));
            }

            if (!new PageClient(_context).PageUrlValidation(url))
            {
                msg = (type != "page") 
                    ? _localizer["CategoryCannotHaveThisPath", url].Value
                    : _localizer["PageCannotHaveThisPath", url].Value;
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = msg
                }));
            }

            try
            {
                _page = new Pages { WebsiteLanguageId = websiteLanguageId, PageTemplateId = pageTemplateId, Parent = parent, Url = url, Title = title, Keywords = keywords, Description = description, AlternateGuid = alternateGuid, Active = true, Name = name };
                _context.Pages.Add(_page);
                _context.SaveChanges();
            }
            catch
            {
                msg = (type != "page")
                   ? _localizer["CannotCreateTheCategory"].Value
                    : _localizer["CannotCreateThePage"].Value;
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = msg
                }));
            }

            var _pageTemplateFields = _context.PageTemplateFields.Where(ptf => ptf.PageTemplateId == pageTemplateId).ToList();
            foreach (PageTemplateFields PageTemplateField in _pageTemplateFields)
            {
                var pr = new Site.Data.PageResources { PageId = _page.Id, PageTemplateFieldId = PageTemplateField.Id, Text = PageTemplateField.DefaultValue };
                _context.PageResources.Add(pr);
            }

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                DeletePage(_page.Id, type);

                msg = (type != "page") 
                    ? _localizer["CannotCreateTheCategory"].Value
                    : _localizer["CannotCreateThePage"].Value;
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = msg
                }));
            }

            var _pageResources = _context.PageResources.Join(_context.PageTemplateFields, PageResources => PageResources.PageTemplateFieldId, PageTemplateFields => PageTemplateFields.Id, (PageResources, PageTemplateFields) => new { PageResources, PageTemplateFields }).Where(x => x.PageResources.PageId == _page.Id).OrderBy(x => x.PageTemplateFields.CustomOrder).ToList();

            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;
            foreach (var i in _pageResources)
            {
                childRow = new Dictionary<string, object>
                {
                    { "Id", i.PageResources.Id},
                    { "PageId", i.PageResources.PageId},
                    { "PageTemplateFieldId", i.PageResources.PageTemplateFieldId},
                    { "Text", i.PageResources.Text},
                    { "Heading", i.PageTemplateFields.Heading},
                    { "Type", i.PageTemplateFields.Type}
                };
                parentRow.Add(childRow);
            }

            var PTU = _context.PageTemplateUploads.Where(ptu => ptu.PageTemplateId == pageTemplateId).OrderBy(PageTemplateUploads => PageTemplateUploads.CustomOrder).ToList();

            List<Dictionary<string, object>> PtuParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> PtuChildRow;
            foreach (PageTemplateUploads PageTemplateUpload in PTU)
            {
                PtuChildRow = new Dictionary<string, object>
                {
                    { "Id", PageTemplateUpload.Id},
                    { "PageTemplateId", PageTemplateUpload.PageTemplateId},
                    { "CallName", PageTemplateUpload.CallName},
                    { "Heading", PageTemplateUpload.Heading},
                    { "MimeTypes", PageTemplateUpload.MimeTypes},
                    { "FileExtensions", PageTemplateUpload.FileExtensions},
                    { "MinFiles", PageTemplateUpload.MinFiles},
                    { "MaxFiles", PageTemplateUpload.MaxFiles},
                    { "MaxSize", PageTemplateUpload.MaxSize},
                    { "Width", PageTemplateUpload.Width},
                    { "Height", PageTemplateUpload.Height},
                    {"CustomOrder", PageTemplateUpload.CustomOrder}
                };
                PtuParentRow.Add(PtuChildRow);
            }

            msg = (type != "page") 
                ? _localizer["CategoryCreated"].Value
                : _localizer["PageCreated"].Value;
            return Ok(Json(new
            {
                messageType = "success",
                message = msg,
                pageId = _page.Id,
                active = _page.Active,
                rootPage = false,
                data = parentRow,
                uploads = PtuParentRow
            }));
        }

        [Route("/spine-api/page/update")]
        [HttpPost]
        public IActionResult Post(Pages Page, string type)
        {
            try
            {
                if (Page.Url == null) { Page.Url = ""; }
                if (Page.Keywords == null) { Page.Keywords = ""; }
                if (Page.Description == null) { Page.Description = ""; }

                Pages _page = new PageClient(_context).GetPageByWebsiteLanguageIdAndNameAndTypeNotByPageId(websiteLanguageId, Page.Name, type, Page.Id);
                if (_page != null)
                {
                    string msg = (type != "page") 
                        ? _localizer["CategoryNameAlreadyExist"].Value
                        : _localizer["PageNameAlreadyExist"].Value;
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = msg
                    }));
                }

                if (!new PageClient(_context).PageUrlValidation(Page.Url))
                {
                    string msg = (type != "page")
                       ? _localizer["CannotCreateTheCategory"].Value
                        : _localizer["CannotCreateThePage"].Value;
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = msg
                    }));
                }

                _page = new Pages { Id = Page.Id, Name = Page.Name, Url = Page.Url, Title = Page.Title, Keywords = Page.Keywords, Description = Page.Description };
                _context.Pages.Attach(_page);
                _context.Entry(_page).Property(p => p.Name).IsModified = true;
                _context.Entry(_page).Property(p => p.Title).IsModified = true;
                _context.Entry(_page).Property(p => p.Keywords).IsModified = true;
                _context.Entry(_page).Property(p => p.Description).IsModified = true;

                Websites _website = _context.Pages.Join(_context.Websites, Pages => Pages.AlternateGuid, Websites => Websites.RootPageAlternateGuid, (Pages, Websites) => new { Pages, Websites })
                                                  .Where(x => x.Pages.Id == Page.Id)
                                                  .Select(x => x.Websites)
                                                  .FirstOrDefault();

                //If it isn't the root page then save url
                if (_website == null)
                {
                    _context.Entry(_page).Property(p => p.Url).IsModified = true;
                }

                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateInformation"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["InformationSuccesUpdated"].Value
            }));
        }

        [Route("/spine-api/page/resources/update")]
        [HttpPost]
        public IActionResult Post([FromBody] List<Site.Data.PageResources> PageResource)
        {
            foreach (var i in PageResource)
            {
                var _pageResource = new Site.Data.PageResources { Id = i.Id, Text = i.Text };
                _context.PageResources.Attach(_pageResource);
                _context.Entry(_pageResource).Property(pr => pr.Text).IsModified = true;
            }

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CannotUpdateResources"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                messageType = "success",
                message = _localizer["ResourcesUpdated"].Value
            });
        }

        [Route("/spine-api/page/files/order/update")]
        [HttpPost]
        public IActionResult UpdateOrder([FromBody] List<PageFiles> PageFiles)
        {
            var count = 0;
            foreach (var i in PageFiles)
            {
                var _pageFile = new PageFiles { Id = i.Id, CustomOrder = ++count };
                _context.PageFiles.Attach(_pageFile);
                _context.Entry(_pageFile).Property(pf => pf.CustomOrder).IsModified = true;
            }

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CannotUpdateOrderFiles"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                messageType = "success",
                message = _localizer["OrderFilesUpdated"].Value
            });
        }

        [Route("/spine-api/page/file/delete")]
        [HttpPost]
        public IActionResult DeleteFile(int Id)
        {
            try
            {
                PageFiles _pageFile = _context.PageFiles.FirstOrDefault(PageFiles => PageFiles.Id == Id);
                _context.PageFiles.Remove(_pageFile);
                _context.SaveChanges();

                Websites _website = _context.WebsiteLanguages.Join(_context.Websites, WebsiteLanguages => WebsiteLanguages.WebsiteId, Websites => Websites.Id, (WebsiteLanguages, Websites) => new { WebsiteLanguages, Websites })
                                                             .Where(x => x.WebsiteLanguages.Id == websiteLanguageId)
                                                             .Select(x => x.Websites)
                                                             .FirstOrDefault();

                var path = _env.WebRootPath + $@"\websites\{_website.Folder}{ _pageFile.OriginalPath.Replace("~/", "/") }";
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                path = _env.WebRootPath + $@"\websites\{_website.Folder}{ _pageFile.CompressedPath.Replace("~/", "/") }";
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CannotDeleteFile"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                messageType = "success",
                message = _localizer["FileDeleted"].Value
            });
        }

        [Route("/spine-api/page/file")]
        [HttpPost]
        public IActionResult UploadFiles()
        {
            Websites _website = new Website(_context).GetWebsiteById(websiteId);

            int FormPageId = int.Parse(Request.Form["PageId"]);

            string originalPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\pages\{FormPageId}";
            if (!Directory.Exists(originalPath))
            {
                Directory.CreateDirectory(originalPath);
            }

            string compressedPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\pages\{FormPageId}";
            if (!Directory.Exists(compressedPath))
            {
                Directory.CreateDirectory(compressedPath);
            }

            //Variable for total size of all files
            long TotalSize = 0;

            var files = Request.Form.Files;
            foreach (var file in files)
            {
                var filename = Request.Form["filename"].ToString().Trim('"');

                //File name validation
                filename = ValidateFileName(filename);

                //Check availibility of file, otherwise a number will be put on the end of the name
                string LocationOriginal = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\pages\{FormPageId}\{filename}";
                LocationOriginal = CheckAvailibiltyFile(LocationOriginal, LocationOriginal, 1);

                //Counting total size of all files
                TotalSize += file.Length;

                using (FileStream fs = System.IO.File.Create(LocationOriginal))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                }

                var LocationCompressed = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\pages\{FormPageId}\{filename}";
                LocationCompressed = CheckAvailibiltyFile(LocationCompressed, LocationCompressed, 1);

                var extension = Path.GetExtension(LocationOriginal).ToLower();
                if (extension == ".png" || extension == ".gif" || extension == ".jpg" || extension == ".jpeg")
                {
                    using (var image = new Bitmap(LocationOriginal))
                    {
                        var resized = new Bitmap(image.Width, image.Height);
                        using (var graphics = Graphics.FromImage(resized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                            var encoderParameters = new EncoderParameters(1);
                            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 75L);

                            Guid FileGuid;
                            switch (extension)
                            {
                                case ".png":
                                    FileGuid = ImageFormat.Png.Guid;
                                    break;
                                case ".gif":
                                    FileGuid = ImageFormat.Gif.Guid;
                                    break;
                                case ".jpg":
                                    FileGuid = ImageFormat.Jpeg.Guid;
                                    break;
                                default: // ".jpeg":
                                    FileGuid = ImageFormat.Jpeg.Guid;
                                    break;
                            }


                            var codec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == FileGuid);
                            resized.Save(LocationCompressed, codec, encoderParameters);
                        }
                    }
                }
                else if (extension == ".pdf")
                {
                    PdfReader reader = new PdfReader(LocationOriginal);
                    PdfStamper stamper = new PdfStamper(reader, new FileStream(LocationCompressed, FileMode.Create), PdfWriter.VERSION_1_5);

                    stamper.FormFlattening = true;
                    stamper.SetFullCompression();
                    stamper.Close();
                }
                else
                {
                    //Support for pdf etc is still under construction
                }

                int FormPageTemplateUploadId = int.Parse(Request.Form["PageTemplateUploadId"]);

                var pf = new PageFiles { PageId = FormPageId, PageTemplateUploadId = FormPageTemplateUploadId, OriginalPath = $@"~/assets/uploads/original/pages/{FormPageId}/{ Path.GetFileName(LocationOriginal) }", CompressedPath = $@"~/assets/uploads/compressed/pages/{FormPageId}/{ Path.GetFileName(LocationCompressed) }", Alt = "", CustomOrder = 0, Active = true };
                _context.PageFiles.Add(pf);
                _context.SaveChanges();
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["FileAdded"].Value
            }));
        }

        [Route("/spine-api/page/file/active")]
        [HttpPost]
        public IActionResult Post([FromBody]PageFiles pageFile)
        {
            var _pageFile = new PageFiles { Id = pageFile.Id, Active = pageFile.Active };
            _context.PageFiles.Attach(_pageFile);
            _context.Entry(_pageFile).Property(PageFiles => PageFiles.Active).IsModified = true;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateFileStatus"].Value
                }));
            }

            var Message = _localizer["FileDeactivated"].Value;
            if (pageFile.Active ?? true)
            {
                Message = _localizer["FileActivated"].Value;
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = Message
            }));
        }

        [Route("/spine-api/page/file/alt")]
        [HttpPost]
        public IActionResult PageFileAltApi([FromBody]PageFiles pageFile)
        {
            var _pageFile = new PageFiles { Id = pageFile.Id, Alt = pageFile.Alt };
            _context.PageFiles.Attach(_pageFile);
            _context.Entry(_pageFile).Property(PageFiles => PageFiles.Alt).IsModified = true;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateFileDescription"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["FileDescriptionUpdated"].Value
            }));
        }

        [Route("/spine-api/page/files")]
        [HttpGet]
        public IActionResult GetFiles(int PageId, int PageTemplateUploadId)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;

            try
            {
                var _website = _context.WebsiteLanguages.Join(_context.Websites, wl => wl.WebsiteId, w => w.Id, (wl, w) => new { wl, w }).SingleOrDefault(x => x.wl.Id == websiteLanguageId);
                var path = $@"/websites/{_website.w.Folder}";

                var _pageFiles = _context.PageFiles.Where(pf => pf.PageTemplateUploadId == PageTemplateUploadId && pf.PageId == PageId).OrderBy(pf => pf.CustomOrder).ToList();

                foreach (PageFiles pageFile in _pageFiles)
                {
                    childRow = new Dictionary<string, object>
                    {
                        { "Id", pageFile.Id},
                        { "PageId", pageFile.PageId},
                        { "PageTemplateUploadId", pageFile.PageTemplateUploadId},
                        { "OriginalPath", path + pageFile.OriginalPath.Replace('\\', '/').Replace("~/", "/")},
                        { "CompressedPath", path + pageFile.CompressedPath.Replace('\\', '/').Replace("~/", "/")},
                        { "Alt", pageFile.Alt},
                        { "CustomOrder", pageFile.CustomOrder},
                        { "Active", pageFile.Active}
                    };
                    parentRow.Add(childRow);
                }
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CannotGetFiles"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                data = parentRow
            });
        }

        [Route("/pages")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/categories")]
        public IActionResult Categories()
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { LocalRedirect("/dashboard"); }

            return View();
        }

        [Route("/breadcrumbs")]
        public IActionResult Breadcrumbs()
        {
            ViewData["eCommerce"] = new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower();

            return View();
        }

        [Route("/pages/add")]
        [Route("/categories/add")]
        [Route("/pages/edit/{id}")]
        [Route("/categories/edit/{id}")]
        public async Task<IActionResult> Add()
        {
            string type = "page";
            string[] urlParts = Request.Path.Value.Split('/');
            if (urlParts[1].ToString() == "categories")
            {
                if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { LocalRedirect("/dashboard"); }

                type = "eCommerceCategory";

                ViewData["Title"] = _localizer["AddCategory"].Value;
                if (RouteData.Values["id"] != null)
                {
                    ViewData["Title"] = _localizer["EditCategory"].Value;
                }
            }
            else
            {
                ViewData["Title"] = _localizer["AddPage"].Value;
                if (RouteData.Values["id"] != null)
                {
                    ViewData["Title"] = _localizer["EditPage"].Value;
                }
            }

            Models.Page.ViewModels.PageAddViewModel vm = new Models.Page.ViewModels.PageAddViewModel()
            {
                Url = new Website(_context).GetWebsiteUrl(websiteLanguageId),
                PageTemplates = await _context.WebsiteLanguages.Join(_context.Websites, WebsiteLanguages => WebsiteLanguages.WebsiteId, Websites => Websites.Id, (WebsiteLanguages, Websites) => new { WebsiteLanguages, Websites })
                                                               .Join(_context.PageTemplates.Where(PageTemplates => PageTemplates.Type == type), x => x.Websites.Id, PageTemplates => PageTemplates.WebsiteId, (x, PageTemplates) => new { x.WebsiteLanguages, PageTemplates })
                                                               .Where(x => x.WebsiteLanguages.Id == websiteLanguageId)
                                                               .OrderBy(x => x.PageTemplates.Name).Select(x => new PageTemplates()
                                                               {
                                                                   Id = x.PageTemplates.Id,
                                                                   Name = x.PageTemplates.Name,
                                                               }).ToListAsync()
            };

            return View(vm);
        }
    }
}