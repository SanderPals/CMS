using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Site.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Site.Models.Page;
using Microsoft.Extensions.Localization;
using Microsoft.ApplicationInsights.AspNetCore;
using System.Reflection;
using System.Resources;

namespace Site.Models
{
    public class Data : Controller
    {
        SiteContext _context;

        public Data(SiteContext context)
        {
            _context = context;
        }

        public class DataBundle
        {
            public DataItems DataItem { get; set; }
            public DataItemResources DataItemResource { get; set; }
            public IEnumerable<DataItemResources> DataItemResources { get; set; }
            public IEnumerable<DataItems> DataItems { get; set; }
            public DataTemplates DataTemplate { get; set; }
            public DataTemplateFields DataTemplateField { get; set; }
            public IEnumerable<DataTemplateFields> DataTemplateFields { get; set; }
        }

        public class DataItemResourcesUpdate
        {
            public object Id { get; set; }
            public int DataItemId { get; set; }
            public int DataTemplateFieldId { get; set; }
            public object Text { get; set; }
            public string Heading { get; set; }
            public string Type { get; set; }
        }

        public class DataUpdate
        {
            public DataItems Item { get; set; }
            public List<DataItemResourcesUpdate> Resources { get; set; }
        }

        public IEnumerable<DataTemplates> GetDataTemplatesByMenuTypeAndWebsiteId(string menuType, int websiteId)
        {
            return _context.DataTemplates.Where(DataTemplates => DataTemplates.WebsiteId == websiteId)
                                         .Where(DataTemplates => DataTemplates.MenuType == menuType)
                                         .OrderBy(DataTemplates => DataTemplates.CustomOrder);
        }

        public IEnumerable<DataBundle> GetDataBundlesByDataTemplateIdAndWebsiteLanguageId(int id, int websitelanguageId)
        {
            return _context.DataItems.Join(_context.DataTemplates, DataItems => DataItems.DataTemplateId, DataTemplates => DataTemplates.Id, (DataItems, DataTemplates) => new { DataItems, DataTemplates })
                                     .Where(x => x.DataItems.DataTemplateId == id)
                                     .Where(x => x.DataItems.WebsiteLanguageId == websitelanguageId)
                                     .Select(x => new DataBundle
                                     {
                                         DataItem = x.DataItems,
                                         DataTemplate = x.DataTemplates
                                     })
                                     .OrderByDescending(x => x.DataItem.CustomOrder);
        }

        public DataTemplates GetDataTemplateById(int id)
        {
            return _context.DataTemplates.FirstOrDefault(DataTemplates => DataTemplates.Id == id);
        }

        public IQueryable<DataTemplateUploads> GetDataTemplateUploads(int id)
        {
            return _context.DataTemplateUploads.Where(DataTemplateUploads => DataTemplateUploads.DataTemplateId == id)
                                               .OrderBy(DataTemplateUploads => DataTemplateUploads.CustomOrder);
        }

        public IEnumerable<DataItems> GetDataItemsByDataTemplateId(int id)
        {
            return _context.DataTemplates.Join(_context.DataItems, DataTemplates => DataTemplates.Id, DataItems => DataItems.DataTemplateId, (DataTemplates, DataItems) => new { DataTemplates, DataItems })
                                         .Where(x => x.DataTemplates.Id == id)
                                         .Select(x => x.DataItems);
        }

        public IEnumerable<DataBundle> GetDataTemplateFieldAndDataItemResources(int dataItemId, int dataTemplateId)
        {
            return _context.DataTemplateFields.GroupJoin(_context.DataItemResources.Where(DataItemResources => DataItemResources.DataItemId == dataItemId), DataTemplateFields => DataTemplateFields.Id, DataItemResources => DataItemResources.DataTemplateFieldId, (DataTemplateFields, DataItemResources) => new { DataTemplateFields, DataItemResources })
                                              .Where(x => x.DataTemplateFields.DataTemplateId == dataTemplateId)
                                              .OrderBy(x => x.DataTemplateFields.CustomOrder)
                                              .Select(x => new DataBundle()
                                              {
                                                  DataTemplateField = x.DataTemplateFields,
                                                  DataItemResources = x.DataItemResources
                                              })
                                              .ToList();
        }

        public IQueryable<DataBundle> GetDataTemplateAndDataItems(int websiteId, int websiteLanguageId) {
            return _context.DataTemplates.GroupJoin(_context.DataItems.Where(DataItems => DataItems.WebsiteLanguageId == websiteLanguageId).OrderBy(DataItems => DataItems.Title), DataTemplates => DataTemplates.Id, DataItems => DataItems.DataTemplateId, (DataTemplates, DataItems) => new { DataTemplates, DataItems })
                                         .Select(x => new DataBundle()
                                         {
                                             DataTemplate = x.DataTemplates,
                                             DataItems = x.DataItems
                                         })
                                         .OrderBy(x => x.DataTemplate.Name)
                                         .Where(x => x.DataTemplate.WebsiteId == websiteId && x.DataTemplate.DetailPage == true);
        }

        public IQueryable<DataBundle> GetDataTemplateAndDataItemsByDataTemplateId(int dataTemplateId)
        {
            return _context.DataTemplates.GroupJoin(_context.DataItems.OrderBy(DataItems => DataItems.Title), DataTemplates => DataTemplates.Id, DataItems => DataItems.DataTemplateId, (DataTemplates, DataItems) => new { DataTemplates, DataItems })
                                         .Select(x => new DataBundle()
                                         {
                                             DataTemplate = x.DataTemplates,
                                             DataItems = x.DataItems
                                         })
                                         .OrderBy(x => x.DataTemplate.Name)
                                         .Where(x => x.DataTemplate.Id == dataTemplateId);
        }

        public DataBundle GetDataItemAndDataTemplate(int id)
        {
            return _context.DataItems.Join(_context.DataTemplates, DataItems => DataItems.DataTemplateId, DataTemplates => DataTemplates.Id, (DataItems, DataTemplates) => new { DataItems, DataTemplates })
                                     .Select(x => new DataBundle()
                                     {
                                         DataItem = x.DataItems,
                                         DataTemplate = x.DataTemplates
                                     })
                                     .FirstOrDefault(x => x.DataItem.Id == id);
        }

        public DataTemplates GetDataTemplateByPageAlternateGuid(string pageAlternateGuid)
        {
            return _context.DataTemplates.FirstOrDefault(DataTemplates => DataTemplates.PageAlternateGuid == pageAlternateGuid);
        }

        public IEnumerable<DataItemResources> GetLinkedDataItemResources(int id) {
            return _context.DataItems.Where(DataItems => DataItems.Id == id)
                                     .Join(_context.DataTemplates, DataItems => DataItems.DataTemplateId, DataTemplates => DataTemplates.Id, (DataItems, DataTemplates) => new { DataItems, DataTemplates })
                                     .Join(_context.DataTemplateFields, x => x.DataTemplates.Id, DataTemplateFields => DataTemplateFields.LinkedToDataTemplateId, (x, DataTemplateFields) => new { x.DataItems, x.DataTemplates, DataTemplateFields })
                                     .GroupJoin(_context.DataItemResources.Where(x => x.Text == id.ToString()), x => x.DataTemplateFields.Id, DataItemResources => DataItemResources.DataTemplateFieldId, (x, DataItemResources) => new { x.DataItems, x.DataTemplates, x.DataTemplateFields, DataItemResources })
                                     .SelectMany(x => x.DataItemResources);
        }

        public DataItems GetDataItemByAlternateGuidAndWebsiteLanguageId(int websitelanguageId, string alternateGuid)
        {
            return _context.DataItems.FirstOrDefault(DataItems => DataItems.WebsiteLanguageId == websitelanguageId && DataItems.AlternateGuid == alternateGuid);
        }

        public DataTemplateSections GetDataTemplateSectionByDataTemplateIdAndId(int dataTemplateId, int id)
        {
            return _context.DataTemplateSections.FirstOrDefault(DataTemplateSections => DataTemplateSections.DataTemplateId == dataTemplateId && DataTemplateSections.Id == id);
        }

        public IQueryable<DataTemplateSections> GetDataTemplateSectionsByDataItemAlternateGuid(string alternateGuid, int websiteLanguageId)
        {
            return _context.DataItems.Where(DataItems => DataItems.AlternateGuid == alternateGuid && DataItems.WebsiteLanguageId == websiteLanguageId)
                                     .Join(_context.DataTemplateSections, DataItems => DataItems.DataTemplateId, DataTemplateSections => DataTemplateSections.DataTemplateId, (DataItems, DataTemplateSections) => new { DataItems, DataTemplateSections })
                                     .Select(x => x.DataTemplateSections);
        }

        public string GetDataItemAlternateGuidById(int id)
        {
            DataItems _dataItem = _context.DataItems.FirstOrDefault(DataItems => DataItems.Id == id);

            return (_dataItem != null) ? _dataItem.AlternateGuid : "";
        }

        public bool InsertLinkedDataItemsByList(int dataItemId, int dataTemplateFieldId, List<string> list)
        {
            foreach(string text in list)
            {
                DataItems _dataItem = _context.DataItems.FirstOrDefault(DataItems => DataItems.Id == Int32.Parse(text));
                if (_dataItem != null)
                {
                    InsertDataItemResource(dataItemId, dataTemplateFieldId, text);
                }
            }

            return true;
        }

        public bool InsertDataItemResource(int dataItemId, int dataTemplateFieldId, string text)
        {
            DataItemResources _dataItemResource = new DataItemResources { DataItemId = dataItemId, DataTemplateFieldId = dataTemplateFieldId, Text = text };
            _context.DataItemResources.Add(_dataItemResource);
            _context.SaveChanges();

            return true;
        }

        public DataItems InsertDataItem(int websiteLanguageId, int dataTemplateId, string title, string subtitle, string text, string htmlEditor, DateTime publishDate, DateTime fromDate, DateTime toDate, bool active, string pageUrl, string pageTitle, string pageKeywords, string pageDescription, int customOrder)
        {
            string alternateGuid = ValidateNewAlternateGuid(Guid.NewGuid().ToString());

            DataItems _dataItem = new DataItems { WebsiteLanguageId = websiteLanguageId, DataTemplateId = dataTemplateId, Title = title, Subtitle = subtitle, Text = text, HtmlEditor = htmlEditor, PublishDate = publishDate, FromDate = fromDate, ToDate = toDate, Active = active, PageUrl =pageUrl, PageTitle = pageTitle, PageKeywords = pageKeywords, PageDescription = pageDescription, CustomOrder = customOrder, AlternateGuid = alternateGuid };
            _context.DataItems.Add(_dataItem);
            _context.SaveChanges();

            return _dataItem;
        }

        public bool DeleteResourceByDataTemplateFieldIdAndDataItemId(int dataTemplateFieldId, int dataItemId)
        {
            _context.DataItemResources.RemoveRange(_context.DataItemResources.Where(DataItemResources => DataItemResources.DataTemplateFieldId == dataTemplateFieldId && DataItemResources.DataItemId == dataItemId));
            _context.SaveChanges();

            return true;
        }

        public bool DeleteDataItemAndLinkedDataItemResources(int id)
        {
            IEnumerable<DataItemResources> _dataItemResources = GetLinkedDataItemResources(id);
            foreach(DataItemResources dataItemResource in _dataItemResources)
            {
                _context.DataItemResources.Remove(dataItemResource);
            }

            return DeleteDataItem(id);
        }

        public bool DeleteDataItem(int id)
        {
            _context.DataItems.Remove(_context.DataItems.FirstOrDefault(DataItems => DataItems.Id == id));
            _context.SaveChanges();

            return true;
        }

        public ObjectResult UpdateOrInsert(DataUpdate dataUpdate, int websiteLanguageId)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Data", Assembly.GetExecutingAssembly());

            if (!new PageClient(_context).PageUrlValidation(dataUpdate.Item.PageUrl))
            {
                return StatusCode(400, Json(new
                {
                        messageType = "warning",
                        message = string.Format(resourceManager.GetString("ItemCannotHaveSlug"), dataUpdate.Item.PageUrl)
                }));
            }

            bool insert = dataUpdate.Item.Id == 0 ? true : false;
            if (dataUpdate.Item != null) { dataUpdate.Item = UpdateOrInsertDataItem(dataUpdate.Item, websiteLanguageId); }

            foreach (DataItemResourcesUpdate dataItemResourcesUpdate in dataUpdate.Resources)
            { 
                if (dataItemResourcesUpdate.Type.ToLower() == "selectlinkedto")
                {
                    UpdateLinkedDataItemResources(dataItemResourcesUpdate, dataUpdate.Item.Id);
                }
                else
                {
                    UpdateOrInsertDataItemResource(dataItemResourcesUpdate, dataUpdate.Item.Id);
                }
            }

            return Ok(Json(new
            {
                item = dataUpdate.Item,
                messageType = "success",
                message = insert ? resourceManager.GetString("ItemAdded") : resourceManager.GetString("ResourcesUpdated")
            }));
        }

        public DataItems UpdateOrInsertDataItem(DataItems dataItem, int websiteLanguageId)
        {
            if (dataItem.Id != 0)
            {
                UpdateDataItem(dataItem);

                return dataItem;
            }
            else
            {
                return InsertDataItem(websiteLanguageId, dataItem.DataTemplateId, dataItem.Title, dataItem.Subtitle, dataItem.Text, dataItem.HtmlEditor, dataItem.PublishDate, dataItem.FromDate, dataItem.ToDate, false, dataItem.PageUrl, dataItem.PageTitle, dataItem.PageKeywords, dataItem.PageDescription, 0);
            }
        }

        public bool UpdateDataItemActive(int id, bool active)
        {
            DataItems _dataItem = new DataItems { Id = id, Active = active };
            _context.DataItems.Attach(_dataItem);
            _context.Entry(_dataItem).Property(DataItem => DataItem.Active).IsModified = true;
            _context.SaveChanges();

            return true;
        }

        public bool UpdateDataItem(DataItems dataItem)
        {
            _context.DataItems.Update(dataItem);
            _context.SaveChanges();

            return true;
        }

        public bool UpdateLinkedDataItemResources(DataItemResourcesUpdate dataItemResourcesUpdate, int dataItemId)
        {
            DeleteResourceByDataTemplateFieldIdAndDataItemId(dataItemResourcesUpdate.DataTemplateFieldId, dataItemId);

            List<string> list = JsonConvert.DeserializeObject<List<string>>(dataItemResourcesUpdate.Text.ToString()) as List<string>;
            InsertLinkedDataItemsByList(dataItemId, dataItemResourcesUpdate.DataTemplateFieldId, list);

            return true;
        }

        public bool UpdateDataItemResourceText(int id, string text)
        {
            DataItemResources _dataItemResource = _context.DataItemResources.FirstOrDefault(x => x.Id == id);
            _dataItemResource.Text = text;
            _context.DataItemResources.Update(_dataItemResource);
            _context.SaveChanges();

            return true;
        }

        public bool UpdateOrInsertDataItemResource(DataItemResourcesUpdate dataItemResourcesUpdate, int dataItemId) {
            if (Int32.Parse(dataItemResourcesUpdate.Id.ToString()) != 0)
            {
                UpdateDataItemResourceText(Int32.Parse(dataItemResourcesUpdate.Id.ToString()), dataItemResourcesUpdate.Text.ToString());
            }
            else
            {
                InsertDataItemResource(dataItemId, dataItemResourcesUpdate.DataTemplateFieldId, dataItemResourcesUpdate.Text.ToString());
            }

            return true;
        }

        public List<Dictionary<string, object>> CreateDataBundleOptionsArrayList(IQueryable<DataBundle> dataBundles)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;

            foreach (DataBundle dataBundle in dataBundles)
            {
                List<Dictionary<string, object>> dataItemsParentRow = new List<Dictionary<string, object>>();
                Dictionary<string, object> dataItemsChildRow;

                foreach (DataItems dataItem in dataBundle.DataItems)
                {
                    dataItemsChildRow = new Dictionary<string, object>
                    {
                        {"id", dataItem.AlternateGuid},
                        {"text", dataItem.Title}
                    };

                    dataItemsParentRow.Add(dataItemsChildRow);
                }

                childRow = new Dictionary<string, object>
                {
                    {"text", dataBundle.DataTemplate.Name},
                    {"children", dataItemsParentRow}
                };

                parentRow.Add(childRow);
            }

            return parentRow;
        }

        public List<Dictionary<string, object>> CreateDataTemplateSectionOptionsArrayList(IQueryable<DataTemplateSections> dataTemplateSections)
        {
            List<Dictionary<string, object>> sectionsParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> sectionsChildRow;
            foreach (DataTemplateSections dataTemplateSection in dataTemplateSections.Where(DataTemplateSections => DataTemplateSections.Type.ToLower() == "section"))
            {
                sectionsChildRow = new Dictionary<string, object>
                {
                    {"id", dataTemplateSection.Id},
                    {"text", dataTemplateSection.Name}
                };
                sectionsParentRow.Add(sectionsChildRow);
            }

            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;

            if (sectionsParentRow.Count() != 0)
            {
                childRow = new Dictionary<string, object>
                {
                    {"text", "Sections"},
                    {"children", sectionsParentRow}
                };
                parentRow.Add(childRow);
            }

            foreach (DataTemplateSections dataTemplateSection in dataTemplateSections.Where(DataTemplateSections => DataTemplateSections.Type.ToLower() == "dataFilter"))
            {
                IQueryable<DataBundle> _dataBundles = new Data(_context).GetDataTemplateAndDataItemsByDataTemplateId(dataTemplateSection.LinkedToDataTemplateId);
                foreach (DataBundle dataBundle in _dataBundles)
                {
                    List<Dictionary<string, object>> dataItemsParentRow = new List<Dictionary<string, object>>();
                    Dictionary<string, object> dataItemsChildRow;

                    foreach (DataItems dataItem in dataBundle.DataItems)
                    {
                        dataItemsChildRow = new Dictionary<string, object>
                        {
                            {"id", dataTemplateSection.Id + ":" + dataItem.AlternateGuid},
                            {"text", dataItem.Title}
                        };

                        dataItemsParentRow.Add(dataItemsChildRow);
                    }

                    childRow = new Dictionary<string, object>
                    {
                        {"text", "Filters " + dataBundle.DataTemplate.Name.ToLower()},
                        {"children", dataItemsParentRow}
                    };

                    parentRow.Add(childRow);
                }
            }

            return parentRow;
        }

        public List<Dictionary<string, object>> GetDataOptionsArrayList(int websiteId, int websiteLanguageId)
        {
            return CreateDataBundleOptionsArrayList(GetDataTemplateAndDataItems(websiteId, websiteLanguageId));
        }

        public List<Dictionary<string, object>> GetSectionOptionsArrayList(string alternateGuid, int websiteLanguageId)
        {
            IQueryable<DataTemplateSections> _dataTemplateSections = GetDataTemplateSectionsByDataItemAlternateGuid(alternateGuid, websiteLanguageId);
            return CreateDataTemplateSectionOptionsArrayList(_dataTemplateSections);
        }

        public string ValidateNewAlternateGuid(string alternateGuid)
        {
            DataItems _dataItem = _context.DataItems.FirstOrDefault(DataItems => DataItems.AlternateGuid == alternateGuid);
            if (_dataItem != null)
            {
                return ValidateNewAlternateGuid(Guid.NewGuid().ToString());
            }
            else
            {
                return alternateGuid;
            }
        }
    }
}