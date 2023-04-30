using Microsoft.AspNetCore.Mvc;
using Site.Data;

namespace Site.Models
{
    public class ApiKey : Controller
    {
        private readonly SiteContext _context;

        public ApiKey(SiteContext context)
        {
            _context = context;
        }

        public void UpdateApiKey(ApiKeys apiKey)
        {
            _context.ApiKeys.Update(apiKey);
            _context.SaveChanges();
        }
    }
}