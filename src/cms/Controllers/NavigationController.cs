using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization;
using static Site.Models.Navigation;

namespace Site.Controllers
{
    public class NavigationController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<NavigationController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public NavigationController(SiteContext context, UserManager<ApplicationUser> userManager, IHostingEnvironment env, IHttpContextAccessor httpContextAccessor, IHttpContextAccessor contextAccessor,
            IStringLocalizer<NavigationController> localizer)
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

        [Route("/spine-api/navigation/item")]
        [HttpGet]
        public IActionResult GetItem(int id)
        {
            try
            {
                return Ok(Json(new Navigation(_context).GetNavigationItemByIdAndWebsiteLanguageId(id, websiteLanguageId)));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["SomethingWrong"].Value
                }));
            }
        }

        [Route("/spine-api/navigation")]
        [HttpGet]
        public IActionResult GetList(int id)
        {
            try
            {
                List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
                Dictionary<string, object> childRow;

                Navigation navigation = new Navigation(_context);
                NavigationBundle _navigationBundle = navigation.GetNavigationAndNavigationItems(id, websiteId, websiteLanguageId).FirstOrDefault();
                if (_navigationBundle != null)
                {
                    foreach (NavigationItems navigationItem in _navigationBundle.NavigationItems)
                    {
                        childRow = new Dictionary<string, object>
                        {
                            {"id", navigationItem.Id},
                            {"customOrder", navigationItem.CustomOrder},
                            {"name", navigationItem.Name},
                            {"parent", navigationItem.Parent}
                        };
                        parentRow.Add(childRow);
                    }
                }

                //If there are no data items, then we still need the template information.
                string name = "";
                int maxDepth = 0;
                if (_navigationBundle == null)
                {
                    Navigations _navigation = navigation.GetNavigationById(id);

                    //Does the review template exists?
                    if (_navigation != null)
                    {
                        name = _navigation.Name;
                        maxDepth = _navigation.MaxDepth;
                    }
                    else
                    {
                        return StatusCode(400, Json(new
                        {
                            redirect = "/dashboard"
                        }));
                    }
                }
                else
                {
                    name = _navigationBundle.Navigation.Name;
                    maxDepth = _navigationBundle.Navigation.MaxDepth;
                }

                return Ok(Json(new
                {
                    data = parentRow,
                    maxDepth = maxDepth,
                    linkToOptions = navigation.GetLinkToOptions(websiteId),
                    name = name
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowList"].Value
                }));
            }
        }

        [Route("/spine-api/navigation/items/update")]
        [HttpPost]
        public IActionResult UpdateList([FromBody] List<NavigationItems> navigationItems)
        {
            try
            {
                foreach (NavigationItems navigationItem in navigationItems)
                {
                    NavigationItems _navigationItem = new NavigationItems { Id = navigationItem.Id, CustomOrder = navigationItem.CustomOrder, Parent = navigationItem.Parent };
                    _context.NavigationItems.Attach(_navigationItem);
                    _context.Entry(_navigationItem).Property(NavigationItems => NavigationItems.CustomOrder).IsModified = true;
                    _context.Entry(_navigationItem).Property(NavigationItems => NavigationItems.Parent).IsModified = true;
                }

                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["WeCouldNotSaveTheOrder"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["OrderIsSaved"].Value
            }));
        }

        [Route("/spine-api/navigation/item/update")]
        [HttpPost]
        public IActionResult Post([FromBody] NavigationItems navigationItem)
        {
            string errorMessage = _localizer["WeCouldNotUpdateTheLink"].Value,
                   successMessage = _localizer["LinkIsUpdated"].Value;
            if (navigationItem.Id == 0) {
                errorMessage = _localizer["CouldNotAddLink"].Value;
                successMessage = _localizer["LinkHasBeenAdded"].Value;
            }

            try
            {
                Navigation navigation = new Navigation(_context);
                if (navigationItem != null) { navigationItem = navigation.UpdateOrInsertNavigationItem(navigationItem, websiteLanguageId, websiteId); }

                //If item is null, then the item didn't pass the validation
                if (navigationItem == null)
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = errorMessage
                    }));
                }

                return Ok(Json(new
                {
                    messageType = "success",
                    message = successMessage
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = errorMessage
                }));
            }
        }

        [Route("/spine-api/navigation/item/delete")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                new Navigation(_context).DeleteNavigationItemAndChildNavigationItems(id, websiteLanguageId);

                return Ok(Json(new
                {
                    messageType = "success",
                    message = _localizer["ThisLinkIsDeleted"].Value
                }));
            }
            catch(Exception e)
            {
                Console.Write(e);
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotDeleteLink"].Value
                }));
            }
        }

        [Route("/navigation/{id}")]
        public IActionResult Index(int id)
        {
            Navigations _navigation = new Navigation(_context).GetNavigationById(id);
            if (_navigation != null)
            {
                ViewData["Title"] = _navigation.Name;
            }
            else
            {
                return RedirectToRoute("Dashboard");
            }

            return View();
        }
    }
}