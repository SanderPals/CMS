using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Site.Data;

namespace Site.Models.Company
{
    public class Company : Controller
    {
        SiteContext _context;

        public Company(SiteContext context)
        {
            _context = context;
        }

        public Companies GetCompanyByUserId(string UserId)
        {
            return _context.CompanyUsers.Join(_context.Companies, CompanyUsers => CompanyUsers.CompanyId, Companies => Companies.Id, (CompanyUsers, Companies) => new { CompanyUsers, Companies })
                                        .Where(x => x.CompanyUsers.UserId == UserId)
                                        .Select(x => x.Companies)
                                        .FirstOrDefault();
        }

        public int GetCompanyIdByUserId(string userId)
        {
            CompanyUsers _companyUser = _context.CompanyUsers.FirstOrDefault(CompanyUsers => CompanyUsers.UserId == userId);

            return (_companyUser != null) ? _companyUser.CompanyId : 0;
        }

        public bool InsertCompanyUser(int companyId, string userId)
        {
            CompanyUsers _companyUser = new CompanyUsers { CompanyId = companyId, UserId = userId };
            _context.CompanyUsers.Add(_companyUser);
            _context.SaveChanges();

            return true;
        }

        public bool DeleteCompanyUserById(string userId)
        {
            _context.CompanyUsers.RemoveRange(_context.CompanyUsers.Where(CompanyUsers => CompanyUsers.UserId == userId));
            _context.SaveChanges();

            return true;
        }
    }
}
