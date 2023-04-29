using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Site.Data;
using System.Linq;

namespace Site.Models
{
    public class Shipping
    {
        private readonly SiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public Shipping(SiteContext context, UserManager<ApplicationUser> userManager = null, IHttpContextAccessor httpContextAccessor = null, SignInManager<ApplicationUser> signInManager = null)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
        }

        public ShippingClasses GetShippingClassById(int id)
        {
            return _context.ShippingClasses.FirstOrDefault(ShippingClass => ShippingClass.Id == id);
        }

        public IQueryable<ShippingClasses> GetShippingClassesByWebsiteId(int websiteId)
        {
            return _context.ShippingClasses.OrderBy(ShippingClass => ShippingClass.Name).Where(ShippingClass => ShippingClass.WebsiteId == websiteId);
        }
    }
}