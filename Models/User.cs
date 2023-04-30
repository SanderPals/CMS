using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Site.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Site.Models
{
    public class User
    {
        SiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;

        public User(SiteContext context, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public class UserBundle
        {
            public IEnumerable<AspNetRoles> AspNetRoles { get; set; }
            public AspNetUsers AspNetUser { get; set; }
            public IEnumerable<AspNetUserClaims> AspNetUserClaims { get; set; }
            public IEnumerable<AspNetUserRoles> AspNetUserRoles { get; set; }
        }

        public Claim GetClaimByType(string type)
        {
            Claim _claim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == type);

            return _claim;
        }

        public async Task<Claim> RemoveClaimByTypeAsync(string type, string userId)
        {
            Claim _claim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == type);
            if (_claim != null)
            {
                await _userManager.RemoveClaimAsync(await _userManager.FindByIdAsync(userId), _claim);
            }

            return _claim;
        }

        public IEnumerable<AspNetUserClaims> GetUserClaimsByUserId(string userId)
        {
            return  _context.AspNetUsers.GroupJoin(_context.AspNetUserRoles.Join(_context.AspNetRoles, AspNetUserRoles => AspNetUserRoles.RoleId, AspNetRoles => AspNetRoles.Id, (AspNetUserRoles, AspNetRoles) => new { AspNetUserRoles, AspNetRoles }).OrderBy(x => x.AspNetRoles.Name), AspNetUsers => AspNetUsers.Id, y => y.AspNetUserRoles.UserId, (AspNetUsers, y) => new { AspNetUsers, y })
                                        .GroupJoin(_context.AspNetUserClaims, x => x.AspNetUsers.Id, AspNetUserClaims => AspNetUserClaims.UserId, (x, AspNetUserClaims) => new { x.AspNetUsers, x.y, AspNetUserClaims })
                                        .Where(x => x.AspNetUsers.Id == userId)
                                        .Select(x => x.AspNetUserClaims)
                                        .FirstOrDefault();
        }

        public IEnumerable<UserBundle> GetUserBundlesByUserId(string userId)
        {
            return _context.CompanyUsers.Join(_context.AspNetUsers, CompanyUsers => CompanyUsers.UserId, AspNetUsers => AspNetUsers.Id, (CompanyUsers, AspNetUsers) => new { CompanyUsers, AspNetUsers })
                                        .GroupJoin(_context.AspNetUserRoles.Join(_context.AspNetRoles, AspNetUserRoles => AspNetUserRoles.RoleId, AspNetRoles => AspNetRoles.Id, (AspNetUserRoles, AspNetRoles) => new { AspNetUserRoles, AspNetRoles }).OrderBy(x => x.AspNetRoles.Name), x => x.AspNetUsers.Id, y => y.AspNetUserRoles.UserId, (x, y) => new { x.AspNetUsers, x.CompanyUsers, y })
                                        .GroupJoin(_context.AspNetUserClaims, x => x.AspNetUsers.Id, AspNetUserClaims => AspNetUserClaims.UserId, (x, AspNetUserClaims) => new { x.AspNetUsers, x.CompanyUsers, x.y, AspNetUserClaims })
                                        .Where(x => x.CompanyUsers.CompanyId == _context.CompanyUsers.FirstOrDefault(CompanyUsers => CompanyUsers.UserId == userId).CompanyId)
                                        .Select(x => new UserBundle() {
                                            AspNetRoles = x.y.Select(z => z.AspNetRoles),
                                            AspNetUser = x.AspNetUsers,
                                            AspNetUserClaims = x.AspNetUserClaims,
                                            AspNetUserRoles = x.y.Select(z => z.AspNetUserRoles)
                                        });
        }

        public UserBundle GetUserBundleByUserId(string userId)
        {
            return _context.AspNetUsers.GroupJoin(_context.AspNetUserRoles.Join(_context.AspNetRoles, AspNetUserRoles => AspNetUserRoles.RoleId, AspNetRoles => AspNetRoles.Id, (AspNetUserRoles, AspNetRoles) => new { AspNetUserRoles, AspNetRoles }).OrderBy(x => x.AspNetRoles.Name), AspNetUsers => AspNetUsers.Id, y => y.AspNetUserRoles.UserId, (AspNetUsers, y) => new { AspNetUsers, y })
                                       .GroupJoin(_context.AspNetUserClaims, x => x.AspNetUsers.Id, AspNetUserClaims => AspNetUserClaims.UserId, (x, AspNetUserClaims) => new { x.AspNetUsers, x.y, AspNetUserClaims })
                                       .Select(x => new UserBundle()
                                       {
                                           AspNetRoles = x.y.Select(z => z.AspNetRoles),
                                           AspNetUser = x.AspNetUsers,
                                           AspNetUserClaims = x.AspNetUserClaims,
                                           AspNetUserRoles = x.y.Select(z => z.AspNetUserRoles)
                                       }).FirstOrDefault(x => x.AspNetUser.Id == userId);
        }

        public string GetClaimFromUserBundleByType(string type, UserBundle userBundle)
        {
            return userBundle.AspNetUserClaims.FirstOrDefault(AspNetUserClaims => AspNetUserClaims.ClaimType == type).ClaimValue.ToString();
        }

        public string GetFirstRoleIdFromUserBundle(UserBundle userBundle)
        {
            return userBundle.AspNetRoles.FirstOrDefault().Id.ToString();
        }

        public bool LinkUserToCompanyByUserId(string userId, string applicationUserId)
        {
            try
            {
                Company.Company company = new Company.Company(_context);
                int companyId = company.GetCompanyIdByUserId(userId);
                if(companyId != 0)
                {
                    company.InsertCompanyUser(companyId, applicationUserId);
                }
                else
                {
                    DeleteUserById(applicationUserId);
                    return false;
                }
            }
            catch
            {
                DeleteUserById(applicationUserId);
                return false;
            }
            return true;
        }

        public bool DeleteUserById(string id)
        {
            new Company.Company(_context).DeleteCompanyUserById(id);

            AspNetUsers _aspNetUser = new AspNetUsers { Id = id };
            _context.AspNetUsers.Attach(_aspNetUser);
            _context.AspNetUsers.Remove(_aspNetUser);
            _context.SaveChanges();

            return true;
        }

        public string GenerateRandomPassword(PasswordOptions opts = null)
        {
            if (opts == null) opts = new PasswordOptions()
            {
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = false,
                RequiredLength = 8
            };

            string[] randomChars = new[] {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
                "abcdefghijkmnopqrstuvwxyz",    // lowercase
                "0123456789",                   // digits
                "!@$?_-"                        // non-alphanumeric
            };

            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }
    }
}
