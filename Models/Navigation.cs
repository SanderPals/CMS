using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Models.Page;
using Site.Models.Product;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization;
using Microsoft.ApplicationInsights.AspNetCore;
using System.Reflection;
using System.Resources;

namespace Site.Models
{
    public class Navigation : Controller
    {
        SiteContext _context;

        public Navigation(SiteContext context)
        {
            _context = context;
        }

        public class NavigationBundle
        {
            public NavigationItems NavigationItem { get; set; }
            public IEnumerable<NavigationItems> NavigationItems { get; set; }
            public Navigations Navigation { get; set; }
        }

        public NavigationItems GetNavigationItemByIdAndWebsiteLanguageId(int id, int websiteLanguageId)
        {
            return _context.NavigationItems.FirstOrDefault(NavigationItems => NavigationItems.Id == id && NavigationItems.WebsiteLanguageId == websiteLanguageId);
        }

        public IEnumerable<NavigationItems> GetNavigationItemsByParentAndWebsiteLanguageId(int parent, int websiteLanguageId)
        {
            return _context.NavigationItems.Where(NavigationItems => NavigationItems.Parent == parent && NavigationItems.WebsiteLanguageId == websiteLanguageId);
        }

        public IEnumerable<NavigationBundle> GetNavigationAndNavigationItems(int id, int websiteId, int websiteLanguageId)
        {
            return _context.Navigations.GroupJoin(_context.NavigationItems.Where(NavigationItems => NavigationItems.WebsiteLanguageId == websiteLanguageId), Navigations => Navigations.Id, NavigationItems => NavigationItems.NavigationId, (Navigations, NavigationItems) => new { Navigations, NavigationItems })
                                       .Select(x => new NavigationBundle()
                                       {
                                           Navigation = x.Navigations,
                                           NavigationItems = x.NavigationItems.OrderBy(NavigationItems => NavigationItems.CustomOrder).ToList()
                                       })
                                       .Where(x => x.Navigation.Id == id && x.Navigation.WebsiteId == websiteId);
        }

        public IEnumerable<Navigations> GetNavigationsByWebsiteId(int websiteId)
        {
            return _context.Navigations.Where(DataTemplates => DataTemplates.WebsiteId == websiteId)
                                       .OrderBy(ReviewTemplate => ReviewTemplate.Name)
                                       .ToList();
        }

        public IEnumerable<Navigations> GetNavigationsByLinkedToAlternateGuidAndWebsiteId(string LinkedToAlternateGuid, string linkedToType, int websiteId)
        {
            return _context.NavigationItems.Join(_context.Navigations, NavigationItems => NavigationItems.NavigationId, Navigations => Navigations.Id, (NavigationItems, Navigations) => new { NavigationItems, Navigations })
                                           .Where(x => x.NavigationItems.LinkedToAlternateGuid == LinkedToAlternateGuid && x.Navigations.WebsiteId == websiteId && x.NavigationItems.LinkedToType == linkedToType)
                                           .Select(x => x.Navigations);
        }

        public IEnumerable<Navigations> GetNavigationsByLinkedToAlternateGuidAndWebsiteIdOrByFilterAlternateGuidAndWebsiteId(string linkedToAlternateGuid, string filterAlternateGuid, string linkedToType, int websiteId)
        {
            return _context.NavigationItems.Join(_context.Navigations, NavigationItems => NavigationItems.NavigationId, Navigations => Navigations.Id, (NavigationItems, Navigations) => new { NavigationItems, Navigations })
                                           .Where(x => x.NavigationItems.LinkedToAlternateGuid == linkedToAlternateGuid && x.Navigations.WebsiteId == websiteId && x.NavigationItems.LinkedToType == linkedToType || x.NavigationItems.FilterAlternateGuid == filterAlternateGuid && x.Navigations.WebsiteId == websiteId)
                                           .Select(x => x.Navigations);
        }

        public Navigations GetNavigationById(int id)
        {
            return _context.Navigations.FirstOrDefault(Navigation => Navigation.Id == id);
        }

        public NavigationItems InsertNavigationItem(int websiteLanguageId, int navigationId, int parent, string LinkedToType, int LinkedToSectionId, string LinkedToAlternateGuid, string FilterAlternateGuid, string name, string target, string customUrl, int customOrder)
        {
            NavigationItems _navigationItem = new NavigationItems { WebsiteLanguageId = websiteLanguageId, NavigationId = navigationId, Parent = parent, LinkedToType = LinkedToType, LinkedToSectionId = LinkedToSectionId, LinkedToAlternateGuid = LinkedToAlternateGuid, FilterAlternateGuid = FilterAlternateGuid, Name = name, Target = target, CustomUrl = customUrl, CustomOrder = customOrder };
            _context.NavigationItems.Add(_navigationItem);
            _context.SaveChanges();

            return _navigationItem;
        }

        public NavigationItems UpdateOrInsertNavigationItem(NavigationItems navigationItem, int websiteLanguageId, int websiteId)
        {
            navigationItem = CheckNavigationItem(navigationItem, websiteLanguageId, websiteId);
            if (navigationItem == null) { return navigationItem; }

            if (navigationItem.Id != 0)
            {
                navigationItem.WebsiteLanguageId = websiteLanguageId;
                UpdateNavigationItem(navigationItem);

                return navigationItem;
            }
            else
            {
                return InsertNavigationItem(websiteLanguageId, navigationItem.NavigationId, 0, navigationItem.LinkedToType, navigationItem.LinkedToSectionId, navigationItem.LinkedToAlternateGuid, navigationItem.FilterAlternateGuid, navigationItem.Name, navigationItem.Target, navigationItem.CustomUrl, 0);
            }
        }

        public bool UpdateNavigationItem(NavigationItems navigationItem)
        {
            _context.NavigationItems.Update(navigationItem);
            _context.SaveChanges();

            return true;
        }

        public bool DeleteNavigationItemAndChildNavigationItems(int id, int websiteLanguageId)
        {
            DeleteChildNavigationItems(id, websiteLanguageId);

            return DeleteNavigationItem(id, websiteLanguageId);
        }

        public bool DeleteChildNavigationItems(int parent, int websiteLanguageId) {
            IEnumerable<NavigationItems> _navigationItems = GetNavigationItemsByParentAndWebsiteLanguageId(parent, websiteLanguageId);
            foreach (NavigationItems navigationItem in _navigationItems)
            {
                DeleteChildNavigationItems(navigationItem.Id, websiteLanguageId);
            }

            if (_navigationItems != null) { _context.NavigationItems.RemoveRange(_navigationItems); }

            return true;
        }

        public bool DeleteNavigationItem(int id, int websiteLanguageId)
        {
            _context.NavigationItems.Remove(_context.NavigationItems.FirstOrDefault(NavigationItems => NavigationItems.Id == id && NavigationItems.WebsiteLanguageId == websiteLanguageId));
            _context.SaveChanges();

            return true;
        }

        public NavigationItems CheckNavigationItem(NavigationItems navigationItem, int websiteLanguageId, int websiteId)
        {
            navigationItem.Target = ValidateTarget(navigationItem.Target);

            switch (navigationItem.LinkedToType.ToLower())
            {
                case "page":
                case "category":
                    navigationItem.CustomUrl = "";
                    navigationItem = FillFilterAlternateGuids(navigationItem);

                    //Validate linked page
                    return ValidateLinkedPage(navigationItem, websiteLanguageId);
                case "dataitem":
                    navigationItem.CustomUrl = "";
                    navigationItem = FillFilterAlternateGuids(navigationItem);

                    //Validate linked date item
                    return ValidateLinkedDataItem(navigationItem, websiteLanguageId);
                case "product":
                    navigationItem.CustomUrl = "";
                    navigationItem.LinkedToSectionId = 0;
                    navigationItem.FilterAlternateGuid = "";

                    //Validate linked date item
                    return ValidateLinkedProduct(navigationItem, websiteId);
                case "external":
                    navigationItem.LinkedToAlternateGuid = "";
                    navigationItem.LinkedToSectionId = 0;
                    navigationItem.FilterAlternateGuid = "";
                    return navigationItem;
                default: //"nothing"
                    navigationItem.LinkedToType = "nothing";
                    navigationItem.LinkedToAlternateGuid = "";
                    navigationItem.LinkedToSectionId = 0;
                    navigationItem.FilterAlternateGuid = "";
                    navigationItem.CustomUrl = "";
                    return navigationItem;
            }
        }

        public NavigationItems ValidateLinkedPage(NavigationItems navigationItem, int websiteLanguageId) {
            Pages _page = new PageClient(_context).GetPageByAlternateGuidAndWebsiteLanguageId(websiteLanguageId, navigationItem.LinkedToAlternateGuid);
            return _page == null ? null : ValidateLinkedPageSection(navigationItem, _page);
        }

        public NavigationItems ValidateLinkedProduct(NavigationItems navigationItem, int websiteId)
        {
            Products _product = new ProductClient(_context).GetProductByIdAndWebsiteId(Int32.Parse(navigationItem.LinkedToAlternateGuid), websiteId);
            return _product == null ? null : navigationItem;
        }

        public NavigationItems ValidateLinkedDataItem(NavigationItems navigationItem, int websiteLanguageId)
        {
            DataItems _dataItem = new Data(_context).GetDataItemByAlternateGuidAndWebsiteLanguageId(websiteLanguageId, navigationItem.LinkedToAlternateGuid);
            return _dataItem == null ? null : ValidateLinkedDataSection(navigationItem, _dataItem);
        }

        public NavigationItems ValidateLinkedPageSection(NavigationItems navigationItem, Pages _page)
        {
            if (navigationItem.LinkedToSectionId != 0)
            {
                PageTemplateSections _pageTemplateSection = new PageClient(_context).GetPageTemplateSectionByPageTemplateIdAndId(_page.PageTemplateId, navigationItem.LinkedToSectionId);
                if (_pageTemplateSection != null)
                {
                    switch (_pageTemplateSection.Type.ToLower())
                    {
                        case "datafilter":
                            DataItems _dataItem = new Site.Models.Data(_context).GetDataItemByAlternateGuidAndWebsiteLanguageId(_page.WebsiteLanguageId, navigationItem.FilterAlternateGuid);
                            if (_dataItem == null)
                            {
                                return null;
                            }

                            break;
                        default: //"section"
                            navigationItem.FilterAlternateGuid = "";
                            break;
                    }

                    return navigationItem;
                }

                return null;
            }
            else
            {
                navigationItem.LinkedToSectionId = 0;
                navigationItem.FilterAlternateGuid = "";

                return navigationItem;
            }
        }

        public NavigationItems ValidateLinkedDataSection(NavigationItems navigationItem, DataItems _dataItem)
        {
            if (navigationItem.LinkedToSectionId != 0)
            {
                Data data = new Site.Models.Data(_context);
                DataTemplateSections _dataTemplateSections = data.GetDataTemplateSectionByDataTemplateIdAndId(_dataItem.DataTemplateId, navigationItem.LinkedToSectionId);
                if (_dataTemplateSections != null)
                {
                    switch (_dataTemplateSections.Type.ToLower())
                    {
                        case "datafilter":
                            _dataItem = data.GetDataItemByAlternateGuidAndWebsiteLanguageId(_dataItem.WebsiteLanguageId, navigationItem.FilterAlternateGuid);
                            if (_dataItem == null)
                            {
                                return null;
                            }

                            break;
                        default: //"section"
                            navigationItem.FilterAlternateGuid = "";
                            break;
                    }

                    return navigationItem;
                }
            
                return null;
            }
            else
            {
                navigationItem.LinkedToSectionId = 0;
                navigationItem.FilterAlternateGuid = "";

                return navigationItem;
            }
        }

        public string ValidateTarget(string target)
        {
            List<string> validTargets = new List<string>() {
                "_self", "_blank", "_parent", "_top"
            };

            return (validTargets.Any(target.Contains)) ? target : "_self";
        }

        public NavigationItems FillFilterAlternateGuids(NavigationItems navigationItem)
        {
            if (navigationItem.FilterAlternateGuid.Contains(":"))
            {
                navigationItem.LinkedToSectionId = int.Parse(navigationItem.FilterAlternateGuid.Split(':')[0]);
                navigationItem.FilterAlternateGuid = navigationItem.FilterAlternateGuid.Split(':')[1];
            }
            else
            {
                navigationItem.LinkedToSectionId = int.Parse(navigationItem.FilterAlternateGuid);
            }

            return navigationItem;
        }

        public List<Dictionary<string, string>> GetLinkToOptions(int websiteId)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Navigation", Assembly.GetExecutingAssembly());

            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>()
            {
                new Dictionary<string, string>() { { "value", "page" }, { "text", resourceManager.GetString("APage") } },
                new Dictionary<string, string>() { { "value", "dataitem" }, { "text", resourceManager.GetString("ADetailPage") } },
                new Dictionary<string, string>() { { "value", "external" }, { "text", resourceManager.GetString("AnExternalWebsite") } },
                new Dictionary<string, string>() { { "value", "nothing" }, { "text", resourceManager.GetString("Nothing") } }
            };

            //If e-commerce is enabled, then add these options
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() == "true") {
                list.Add(new Dictionary<string, string>() { { "value", "product" }, { "text", resourceManager.GetString("AProduct") } });
                list.Add(new Dictionary<string, string>() { { "value", "category" }, { "text", resourceManager.GetString("ACategory") } });
            }

            return list.OrderBy(x => x["text"]).ToList();
        }
    }
}