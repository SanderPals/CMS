using Site.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Site.Models.Review
{
    public partial class ReviewClient
    {
        public bool DeleteReviewById(int id)
        {
            Reviews _review = new Reviews { Id = id };
            _context.Reviews.Attach(_review);
            _context.Reviews.Remove(_review);
            _context.SaveChanges();

            return true;
        }

        public bool UpdateReviewActive(int id, bool active)
        {
            Reviews _review = new Reviews { Id = id, Active = active };
            _context.Reviews.Attach(_review);
            _context.Entry(_review).Property(r => r.Active).IsModified = true;
            _context.SaveChanges();

            return true;
        }

        public bool UpdateReviewViewedByAdmin(int id, bool viewedByAdmin)
        {
            Reviews _review = new Reviews { Id = id, ViewedByAdmin = viewedByAdmin };
            _context.Reviews.Attach(_review);
            _context.Entry(_review).Property(r => r.ViewedByAdmin).IsModified = true;
            _context.SaveChanges();

            return true;
        }

        public bool InsertReview(string CallName, int WebsiteLanguageId, int LinkedToId, string UserId, string Name, string Email, string Text, byte Rating)
        {
            ReviewTemplates _reviewTemplate = _context.ReviewTemplates.Where(x => x.WebsiteId == 1).FirstOrDefault(x => x.CallName == CallName);

            DateTime UtcTime = DateTime.UtcNow;
            TimeZoneInfo Tzi = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
            DateTime CreatedAt = TimeZoneInfo.ConvertTime(UtcTime, Tzi); // convert from utc to local

            bool Active = true;
            if (_reviewTemplate.CheckBeforeOnline == true)
            {
                Active = false;
            }

            var _review = new Reviews { WebsiteLanguageId = WebsiteLanguageId, LinkedToId = LinkedToId, UserId = UserId, Name = Name, Email = Email, Text = Text, Rating = Rating, Active = Active, CreatedAt = CreatedAt, ViewedByAdmin = false, ReviewTemplateId = _reviewTemplate.Id };
            _context.Reviews.Add(_review);
            _context.SaveChanges();

            return true;
        }

        public ReviewTemplates GetReviewTemplateById(int id)
        {
            return _context.ReviewTemplates.FirstOrDefault(ReviewTemplates => ReviewTemplates.Id == id);
        }

        public IEnumerable<ReviewBundle> GetReviewBundlesByReviewTemplateId(int id)
        {
            return _context.Reviews.Join(_context.ReviewTemplates, Reviews => Reviews.ReviewTemplateId, ReviewTemplates => ReviewTemplates.Id, (Reviews, ReviewTemplates) => new { Reviews, ReviewTemplates })
                                   .Where(x => x.Reviews.ReviewTemplateId == id)
                                   .Select(x => new ReviewBundle
                                   {
                                       Review = x.Reviews,
                                       ReviewTemplate = x.ReviewTemplates
                                   })
                                   .OrderByDescending(x => x.Review.CreatedAt);
        }

        public IEnumerable<ReviewBundle> GetReviewsByWebsiteIdAndCallName(int WebsiteId, string CallName)
        {
            IEnumerable<ReviewBundle> ReviewBundles = null;

            ReviewBundles = _context.Reviews.Join(_context.ReviewTemplates, Reviews => Reviews.ReviewTemplateId, ReviewTemplates => ReviewTemplates.Id, (Reviews, ReviewTemplates) => new { Reviews, ReviewTemplates })
                                            .Where(x => x.Reviews.Active == true)
                                            .Where(x => x.ReviewTemplates.WebsiteId == WebsiteId)
                                            .Where(x => x.ReviewTemplates.CallName == CallName)
                                            .Select(x => new ReviewBundle()
                                            {
                                                Review = x.Reviews,
                                                ReviewTemplate = x.ReviewTemplates
                                            }).ToList();

            return ReviewBundles;
        }

        public IEnumerable<ReviewTemplates> GetReviewTemplatesByWebsiteId(int WebsiteId)
        {
            return _context.ReviewTemplates.Where(ReviewTemplates => ReviewTemplates.Active == true)
                                           .Where(ReviewTemplates => ReviewTemplates.WebsiteId == WebsiteId)
                                           .OrderBy(ReviewTemplate => ReviewTemplate.Name)
                                           .ToList();
        }
    }
}
