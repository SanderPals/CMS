using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Site.Data;
using Site.Models;
using Site.Models.Review;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using System.Linq;
using static Site.Models.Review.ReviewClient;

namespace Site.Controllers
{
    public class ReviewController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<ReviewController> _localizer;

        private int WebsiteId;
        private int WebsiteLanguageId;

        public ReviewController(SiteContext context, UserManager<ApplicationUser> userManager, IHostingEnvironment env, IHttpContextAccessor contextAccessor, IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<ReviewController> localizer)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _contextAccessor = contextAccessor;
            _httpContextAccessor = httpContextAccessor;
            _localizer = localizer;

            WebsiteId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId").Value);
            WebsiteLanguageId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteLanguageId").Value);
        }

        [Route("/spine-api/reviews")]
        [HttpGet]
        public IActionResult GetList(int id)
        {
            List<Dictionary<string, object>> RParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> RChildRow;

            try
            {
                ReviewClient review = new ReviewClient(_context);
                IEnumerable<ReviewBundle> _reviewBundles = review.GetReviewBundlesByReviewTemplateId(id);

                foreach (var Review in _reviewBundles.Select(x => x.Review))
                {
                    string Name = Review.Name;
                    string Email = Review.Email;
                    if (Review.Anonymous == true)
                    {
                        Name = "Anonymous";
                        Email = "";
                    }

                    RChildRow = new Dictionary<string, object>
                    {
                        { "Id", Review.Id},
                        { "WebsiteLanguageId", Review.WebsiteLanguageId},
                        { "LinkedToId", Review.LinkedToId},
                        { "UserId", Review.UserId},
                        { "Name", Name},
                        { "Email", Email},
                        { "Text", Review.Text},
                        { "Rating", Review.Rating},
                        { "Active", Review.Active},
                        { "CreatedAt", Review.CreatedAt},
                        { "ViewedByAdmin", Review.ViewedByAdmin},
                        { "ReviewTemplateId", Review.ReviewTemplateId},
                        { "Anonymous", Review.Anonymous}
                    };
                    RParentRow.Add(RChildRow);
                }


                //If there are not reviews, then we still need template information.
                string name = "";
                bool checkBeforeOnline = false;
                if (_reviewBundles.Count() == 0)
                {
                    ReviewTemplates _reviewTemplate = review.GetReviewTemplateById(id);

                    //Does the review template exists?
                    if (_reviewTemplate != null)
                    {
                        name = _reviewTemplate.Name;
                        checkBeforeOnline = _reviewTemplate.CheckBeforeOnline ?? true;
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
                    name = _reviewBundles.FirstOrDefault().ReviewTemplate.Name;
                    checkBeforeOnline = _reviewBundles.FirstOrDefault().ReviewTemplate.CheckBeforeOnline ?? true;
                }

                return Ok(Json(new
                {
                    name = name,
                    checkBeforeOnline = checkBeforeOnline,
                    reviews = RParentRow
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowListReviews"].Value
                }));
            }
        }

        [Route("/spine-api/review/get")]
        [HttpGet]
        public IActionResult Get(int Id)
        {
            Reviews _review = _context.Reviews.FirstOrDefault(Reviews => Reviews.Id == Id);

            if (_review == null)
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CannotFindReview"].Value
                });
            }
            else
            {
                var _reviewResources = _context.ReviewResources.Join(_context.ReviewTemplateFields, ReviewResources => ReviewResources.ReviewTemplateFieldId, ReviewTemplates => ReviewTemplates.Id, (ReviewResources, ReviewTemplates) => new { ReviewResources, ReviewTemplates }).Where(x => x.ReviewResources.ReviewId == _review.Id).OrderBy(x => x.ReviewTemplates.CustomOrder).ToList();

                List<Dictionary<string, object>> ParentRow = new List<Dictionary<string, object>>();
                Dictionary<string, object> ChildRow;
                foreach (var i in _reviewResources)
                {
                    ChildRow = new Dictionary<string, object>
                    {
                        {"Id", i.ReviewResources.Id},
                        {"PageId", i.ReviewResources.ReviewId},
                        {"PageTemplateFieldId", i.ReviewResources.ReviewTemplateFieldId},
                        {"Text", i.ReviewResources.Text},
                        {"Heading", i.ReviewTemplates.Heading},
                        {"Type", i.ReviewTemplates.Type},
                    };
                    ParentRow.Add(ChildRow);
                }

                var Name = _review.Name;
                var Email = _review.Email;

                if (_review.Anonymous ?? true)
                {
                    Name = "";
                    Email = "";
                }

                if (!_review.ViewedByAdmin ?? true)
                {
                    _review.ViewedByAdmin = true;
                    _context.Reviews.Attach(_review);
                    _context.Entry(_review).Property(r => r.ViewedByAdmin).IsModified = true;

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
                            message = _localizer["SomethingWrong"].Value
                        });
                    }
                }

                return Json(new
                {
                    success = "Valid",
                    websiteLanguageId = _review.WebsiteLanguageId,
                    linkedToId = _review.LinkedToId,
                    userId = _review.UserId,
                    reviewId = _review.Id,
                    name = Name,
                    email = Email,
                    text = _review.Text,
                    rating = _review.Rating,
                    active = _review.Active,
                    createdAt = _review.CreatedAt,
                    viewedByAdmin = _review.ViewedByAdmin,
                    anonymous = _review.Anonymous,
                    reviewTemplateId = _review.ReviewTemplateId,
                    data = ParentRow
                });
            }
        }

        [Route("/spine-api/review/delete")]
        [HttpPost]
        public IActionResult DeleteReview(int id)
        {
            try
            {
                new ReviewClient(_context).DeleteReviewById(id);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotDeleteReview"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["ReviewDeleted"].Value
            }));
        }

        [Route("/spine-api/review/active")]
        [HttpPost]
        public IActionResult UpdateActive([FromBody]Reviews review)
        {
            try
            {
                new ReviewClient(_context).UpdateReviewActive(review.Id, review.Active ?? false);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateReviewStatus"].Value
                }));
            }

            string message = _localizer["ReviewDeactivated"].Value;
            if (review.Active ?? true)
            {
                message = _localizer["ReviewActivated"].Value;
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = message
            }));
        }

        [Route("/spine-api/review/viewedbyadmin")]
        [HttpPost]
        public IActionResult UpdateViewedByAdmin([FromBody]Reviews review)
        {
            try
            {
                new ReviewClient(_context).UpdateReviewViewedByAdmin(review.Id, review.ViewedByAdmin ?? false);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotMarkReview"].Value
                }));
            }

            string message = _localizer["MarkedReviewAsUnread"].Value;
            if (review.ViewedByAdmin ?? true)
            {
                message = _localizer["MarkedReviewAsRead"].Value;
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = message
            }));
        }

        [Route("/spine-api/reviewtemplate/checkbeforeonline")]
        [HttpPost]
        public IActionResult PostViewedByAdmin([FromBody]ReviewTemplates ReviewTemplate)
        {
            var _reviewTemplate = new ReviewTemplates { Id = ReviewTemplate.Id, CheckBeforeOnline = ReviewTemplate.CheckBeforeOnline };
            _context.ReviewTemplates.Attach(_reviewTemplate);
            _context.Entry(_reviewTemplate).Property(rt => rt.CheckBeforeOnline).IsModified = true;

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
                    message = _localizer["CannotUpdate"].Value
                });
            }

            var Message = _localizer["NotCheckReviewsBeforeOnline"].Value;
            if (ReviewTemplate.CheckBeforeOnline ?? true)
            {
                Message = _localizer["CheckReviewsBeforeOnline"].Value;
            }

            return Json(new
            {
                success = "Valid",
                messageType = "success",
                message = Message
            });
        }

        [Route("/reviews/edit/{id}")]
        public IActionResult ReviewEdit()
        {
            ViewBag.WebsiteId = WebsiteId;
            ViewBag.WebsiteLanguageId = WebsiteLanguageId;

            return View();
        }

        [Route("/reviews/{id}")]
        public IActionResult Index(int id)
        {
            ReviewTemplates _reviewTemplate = new ReviewClient(_context).GetReviewTemplateById(id);
            if (_reviewTemplate != null)
            {
                ViewData["Title"] = _reviewTemplate.Name;
            }
            else
            {
                return RedirectToRoute("Dashboard");
            }

            ViewBag.WebsiteId = WebsiteId;
            ViewBag.WebsiteLanguageId = WebsiteLanguageId;

            return View();
        }
    }
}