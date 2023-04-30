using iTextSharp.text;
using iTextSharp.text.exceptions;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Site.Data;
using Site.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Site.Controllers
{
    public class CompanyController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SiteContext _context;
        private readonly IHostingEnvironment _env;
        private readonly IStringLocalizer<CompanyController> _localizer;

        public CompanyController(SiteContext context, 
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<CompanyController> localizer, 
            IHostingEnvironment env = null)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _localizer = localizer;
        }

        [Authorize(Roles = "Administrator, Developer")]
        [Route("/spine-api/company/update")]
        [HttpPost]
        public IActionResult Post(Companies _company)
        {
            if (_company.Company == null) { _company.Company = ""; }
            if (_company.Vat == null) { _company.Vat = ""; }
            if (_company.Coc == null) { _company.Coc = ""; }
            if (_company.Email == null) { _company.Email = ""; }
            if (_company.PhoneNumber == null) { _company.PhoneNumber = ""; }

            _context.Companies.Update(_company);

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotUpdateCompany"]
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["UpdatedCompany"]
            }));
        }

        [Authorize(Roles = "Administrator, Developer")]
        [Route("/company")]
        public IActionResult Index()
        {
            var UserId = _userManager.GetUserId(User);
            Companies _company = _context.Companies.Join(_context.CompanyUsers, Companies => Companies.Id, CompanyUsers => CompanyUsers.CompanyId, (Companies, CompanyUsers) => new { Companies, CompanyUsers })
                                                         .Where(x => x.CompanyUsers.UserId == UserId)
                                                         .Select(x => x.Companies)
                                                         .FirstOrDefault();

            Companies vm = new Companies()
            {
                Id = _company.Id,
                Company = _company.Company,
                Vat = _company.Vat,
                Coc = _company.Coc,
                Email = _company.Email,
                PhoneNumber = _company.PhoneNumber,
            };

            return View(vm);
        }
    }
}