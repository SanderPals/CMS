using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Site.Data;
using Site.Models.Page;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Localization;
using Microsoft.ApplicationInsights.AspNetCore;
using System.Resources;
using System.Reflection;
using System.Threading.Tasks;

namespace Site.Models.Product
{
    public partial class ProductClient : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;

        private readonly ResourceManager _resourceManager;

        public ProductClient(SiteContext context, UserManager<ApplicationUser> userManager = null, IHostingEnvironment env = null, IHttpContextAccessor httpContextAccessor = null, IHttpContextAccessor contextAccessor = null)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _contextAccessor = contextAccessor;

            _resourceManager = new ResourceManager("Site.Resources.Models.Product.ProductClient", Assembly.GetExecutingAssembly());
        }

        public ObjectResult UpdateProductFilesCustomOrder(List<ProductFiles> productFiles)
        {
            int count = 0;
            foreach (ProductFiles productFile in productFiles)
            {
                ProductFiles _productFile = new ProductFiles { Id = productFile.Id, CustomOrder = ++count };
                _context.ProductFiles.Attach(_productFile);
                _context.Entry(_productFile).Property(ProductFile => ProductFile.CustomOrder).IsModified = true;
            }

            _context.SaveChanges();

            return Ok(Json(new
            {
                messageType = "success",
                message = _resourceManager.GetString("OrderFilesUpdated")
            }));
        }

        public ObjectResult UpdateProductFileAlt(ProductFiles productFile)
        {
            ProductFiles _productFile = new ProductFiles { Id = productFile.Id, Alt = productFile.Alt };
            _context.ProductFiles.Attach(_productFile);
            _context.Entry(_productFile).Property(ProductFile => ProductFile.Alt).IsModified = true;
            _context.SaveChanges();

            return Ok(Json(new
            {
                messageType = "success",
                message = _resourceManager.GetString("FileDescriptionUpdated")
            }));
        }

        public ObjectResult UpdateProductFileActive(ProductFiles productFile)
        {
            ProductFiles _productFile = new ProductFiles { Id = productFile.Id, Active = productFile.Active };
            _context.ProductFiles.Attach(_productFile);
            _context.Entry(_productFile).Property(ProductFile => ProductFile.Active).IsModified = true;
            _context.SaveChanges();

            return Ok(Json(new
            {
                messageType = "success",
                message = (_productFile.Active) 
                ? _resourceManager.GetString("FileActivted")
                : _resourceManager.GetString("FileDeactivated")
            }));
        }


        public ObjectResult UpdateOrInsert(ProductUpdate productUpdate, int id)
        {
            if (productUpdate.Item != null) { productUpdate.Item = UpdateOrInsertProduct(productUpdate.Item); }

            foreach (CategorieUpdate categorieUpdate in productUpdate.Categories)
            {
                UpdateProductPages(categorieUpdate, productUpdate.Item.Id);
            }

            return Ok(Json(new
            {
                item = productUpdate.Item,
                messageType = "success",
                message = (id == 0) 
                ? _resourceManager.GetString("ProductAdded")
                :_resourceManager.GetString("ProductUpdated")
            }));
        }

        public ObjectResult InsertFile(HttpRequest httpRequest, int websiteId)
        {
            Websites _website = new Website(_context).GetWebsiteById(websiteId);
            int id = int.Parse(httpRequest.Form["id"]);
            int uploadId = int.Parse(httpRequest.Form["uploadId"]);
            string filename = httpRequest.Form["filename"].ToString().Trim('"');
            string originalPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\products\{id}";
            string compressedPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\products\{id}";
            IFormFileCollection files = httpRequest.Form.Files;

            Dictionary<string, object> res = new File().UploadFile(id, uploadId, filename, originalPath, compressedPath, files);

            new ProductClient(_context).InsertProductFile(id, uploadId, $@"~/assets/uploads/original/products/{id}/{ Path.GetFileName(res["originalPath"].ToString()) }", $@"~/assets/uploads/compressed/products/{id}/{ Path.GetFileName(res["compressedPath"].ToString()) }", "", 0, true);

            return Ok(Json(new
            {
                messageType = "success",
                message = _resourceManager.GetString("FileAdded")
            }));
        }

        public Products UpdateOrInsertProduct(Products product)
        {
            product.Backorders = ValidateBackorder(product.Backorders);
            product.StockStatus = ValidateStockStatus(product.StockStatus);
            product.TaxStatus = ValidateTaxStatus(product.TaxStatus);

            if (product.Id != 0)
            {
                UpdateProduct(product);

                return product;
            }
            else
            {
                return InsertProduct(product.Active ?? false, product.Backorders, product.DownloadExpire, product.DownloadLimit, product.Height, product.Length, product.ManageStock, product.MaxPerOrder, product.Price, product.ProductTemplateId, product.PromoFromDate, product.PromoPrice, product.PromoSchedule ?? false, product.PromoToDate, product.Reviews, product.ShippingClassId, product.Sku, product.StockQuantity, product.StockStatus, product.TaxClassId, product.TaxStatus, product.Weight, product.Width, product.Name, 0);
            }
        }

        public bool UpdateProductPages(CategorieUpdate categorieUpdate, int productId)
        {
            DeleteProductPagesByProductIdAndWebsiteLanguageId(productId, categorieUpdate.Id);

            List<string> categories = JsonConvert.DeserializeObject<List<string>>(categorieUpdate.Categories.ToString()) as List<string>;
            InsertProductPages(productId, categorieUpdate.Id, categories);

            return true;
        }

        public ObjectResult UpdateProductResourcesAndProductPageSettings(List<ProductLanguageUpdate> productLanguageUpdates, int productId, int websiteId, int websiteLanguageId)
        {
            foreach (ProductLanguageUpdate productLanguageUpdate in productLanguageUpdates)
            {
                if (!new PageClient(_context).PageUrlValidation(productLanguageUpdate.Page.Url))
                {
                    StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = string.Format(_resourceManager.GetString("ProductCannotHaveSlug"), productLanguageUpdate.Page.Url)
                    }));
                }

                UpdateOrInsertProductPageSettings(productLanguageUpdate.Page, productId, productLanguageUpdate.Id);

                foreach (ProductResourceUpdate productResourceUpdate in productLanguageUpdate.Fields)
                {
                    UpdateOrInsertProductResource(productResourceUpdate, productId, productLanguageUpdate.Id);
                }
            }

            _context.SaveChanges();

            return Ok(Json(new
            {
                messageType = "success",
                message = _resourceManager.GetString("ResourcesUpdated"),
                resources = ConvertProductResourcesToJson(GetProductBundle(productId, websiteId), websiteLanguageId)
            }));
        }

        public void UpdateOrInsertProductResource(ProductResourceUpdate productResourceUpdate, int productId, int websiteLanguageId)
        {
            if (productResourceUpdate.Id != 0)
            {
                UpdateProductResourceText(productResourceUpdate.Id, productResourceUpdate.Text.ToString());
            }
            else
            {
                InsertProductResource(productId, productResourceUpdate.FieldId, productResourceUpdate.Text.ToString(), websiteLanguageId);
            }
        }

        public void UpdateOrInsertProductPageSettings(ProductPageSettings productPageSetting, int productId, int websiteLanguageId)
        {
            if (productPageSetting.Id != 0)
            {
                UpdateProductPageSetting(productPageSetting);
            }
            else
            {
                InsertProductPageSetting(productPageSetting.Active, productPageSetting.Description, productPageSetting.Keywords, productId, productPageSetting.Title, productPageSetting.Url, websiteLanguageId);
            }
        }

        public void InsertProductPages(int productId, int websiteLanguageId, List<string> categories)
        {
            foreach (string category in categories)
            {
                Pages _page = new PageClient(_context).GetPageByAlternateGuidAndWebsiteLanguageId(websiteLanguageId, category);
                if (_page != null)
                {
                    InsertProductPage(productId, websiteLanguageId, category);
                }
            }
        }

        public void InsertProductPage(int productId, int websiteLanguageId, string pageAlternateGuid)
        {
            ProductPages _productPage = new ProductPages { ProductId = productId, WebsiteLanguageId = websiteLanguageId, PageAlternateGuid = pageAlternateGuid };
            _context.ProductPages.Add(_productPage);
            _context.SaveChanges();
        }

        public void InsertProductResource(int productId, int productFieldId, string text, int websiteLanguageId)
        {
            ProductResources _productResource = new ProductResources { ProductId = productId, ProductFieldId = productFieldId, Text = text, WebsiteLanguageId = websiteLanguageId };
            _context.ProductResources.Add(_productResource);
        }

        public void InsertProductPageSetting(bool active, string description, string keywords, int productId, string title, string url, int websiteLanguageId)
        {
            ProductPageSettings _productPageSettings = new ProductPageSettings { Active = active, Description = description, Keywords = keywords, ProductId = productId, Title = title, Url = url, WebsiteLanguageId = websiteLanguageId };
            _context.ProductPageSettings.Add(_productPageSettings);
        }

        public bool DeleteProductPagesByProductIdAndWebsiteLanguageId(int productId, int websiteLanguageId)
        {
            _context.ProductPages.RemoveRange(_context.ProductPages.Where(ProductPage => ProductPage.ProductId == productId && ProductPage.WebsiteLanguageId == websiteLanguageId));
            _context.SaveChanges();

            return true;
        }

        public ObjectResult DeleteProductFile(int id, int websiteId)
        {
            ProductFiles _productFile = DeleteProductFileById(id);
            Websites _website = new Website(_context).GetWebsiteById(websiteId);

            string[] files = new string[]
            {
                _env.WebRootPath + $@"\websites\{_website.Folder}{ _productFile.OriginalPath.Replace("~/", "/") }",
                _env.WebRootPath + $@"\websites\{_website.Folder}{ _productFile.CompressedPath.Replace("~/", "/") }"
            };
            new File().DeleteFiles(files);

            return Ok(Json(new
            {
                messageType = "success",
                message = _resourceManager.GetString("FileDeleted")
            }));
        }

        public ProductFiles DeleteProductFileById(int id)
        {
            ProductFiles _productFile = _context.ProductFiles.FirstOrDefault(ProductFiles => ProductFiles.Id == id);
            _context.ProductFiles.Remove(_productFile);
            _context.SaveChanges();

            return _productFile;
        }

        public ObjectResult DeleteProduct(int id, int websiteId)
        {
            //Check if the page is in a navigation
            IEnumerable<Navigations> _navigations = new Navigation(_context).GetNavigationsByLinkedToAlternateGuidAndWebsiteId(id.ToString(), "product", websiteId);
            if (_navigations.Count() != 0)
            {
                string navigationsString = string.Join("<br />- ", _navigations.GroupBy(Navigations => Navigations.Id).Select(x => x.FirstOrDefault().Name));
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = string.Format(_resourceManager.GetString("ProductInFollowingNavs"), navigationsString)
                }));
            }

            DeleteProductById(id);

            Websites _website = new Website(_context).GetWebsiteById(websiteId);

            string[] files = {
                _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\products\{id}",
                _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\products\{id}"
            };
            new File().DeleteFiles(files);

            return Ok(Json(new
            {
                messageType = "success",
                message = _resourceManager.GetString("ProductDeleted")
            }));
        }

        public void DeleteProductById(int id)
        {
            Products _product = _context.Products.FirstOrDefault(Product => Product.Id == id);
            _context.Products.Remove(_product);
            _context.SaveChanges();
        }

        public List<Dictionary<string, object>> ConvertProductResourcesToJson(ProductBundle productBundle, int websiteLanguageId)
        {
            List<Dictionary<string, object>> languages = new List<Dictionary<string, object>>();
            foreach (WebsiteLanguages websiteLanguage in productBundle.WebsiteLanguages)
            {
                List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
                foreach (ProductFields productField in productBundle.ProductFields)
                {
                    //if (productField.Type.ToLower() == "selectlinkedto")
                    //{
                    //    IEnumerable<DataItems> _dataItems = data.GetDataItemsByDataTemplateId(dataBundle.DataTemplateField.LinkedToDataTemplateId);

                    //    List<Dictionary<string, object>> diParentRow = new List<Dictionary<string, object>>();
                    //    Dictionary<string, object> diChildRow;
                    //    foreach (DataItems dataItem in _dataItems)
                    //    {
                    //        diChildRow = new Dictionary<string, object>
                    //        {
                    //            { "id", dataItem.Id },
                    //            { "text", dataItem.Title }
                    //        };
                    //        diParentRow.Add(diChildRow);
                    //    }

                    //    List<string> dirParent = new List<string>();
                    //    foreach (DataItemResources dataItemResource in dataBundle.DataItemResources)
                    //    {
                    //        string dirChild = dataItemResource.Text;
                    //        dirParent.Add(dirChild);
                    //    }

                    //    Dictionary<string, object> childRow = new Dictionary<string, object>
                    //    {
                    //        { "Id", Guid.NewGuid().ToString() },
                    //        { "Data", diParentRow },
                    //        { "DataItemId", id },
                    //        { "DataTemplateFieldId", dataBundle.DataTemplateField.Id },
                    //        { "Text", dirParent },
                    //        { "Heading", dataBundle.DataTemplateField.Heading },
                    //        { "Type", dataBundle.DataTemplateField.Type }
                    //    };
                    //}
                    //else
                    //{
                    ProductResources _productResource = (productBundle.ProductResources != null) ? GetProductResourcesByProductFieldIdAndWebsiteLanguageId(productBundle, productField.Id, websiteLanguage.Id) : null;

                    Dictionary<string, object> childRow = new Dictionary<string, object>
                    {
                        { "id", (_productResource != null) ? _productResource.Id : 0 },
                        { "fieldId", productField.Id },
                        { "text", (_productResource != null) ? _productResource.Text : productField.DefaultValue },
                        { "heading", productField.Heading },
                        { "type", productField.Type }
                    };
                    //}
                    parentRow.Add(childRow);
                }

                Dictionary<string, object> child = new Dictionary<string, object>
                {
                    { "id", websiteLanguage.Id },
                    { "code", productBundle.Languages.FirstOrDefault(Languages => Languages.Id == websiteLanguage.LanguageId).Code },
                    { "current", (websiteLanguage.Id == websiteLanguageId) ? true: false },
                    { "fields", parentRow },
                    { "page", (productBundle.ProductPageSettings != null) ? productBundle.ProductPageSettings.FirstOrDefault(x => x.WebsiteLanguageId == websiteLanguage.Id) : new ProductPageSettings() }
                };
                languages.Add(child);
            }

            return languages;
        }

        public List<Dictionary<string, object>> ConvertProductPagesToJson(ProductBundle productBundle, int websiteLanguageId)
        {
            List<Dictionary<string, object>> languages = new List<Dictionary<string, object>>();
            foreach (WebsiteLanguages websiteLanguage in productBundle.WebsiteLanguages)
            {
                List<Dictionary<string, object>> categories = new List<Dictionary<string, object>>();
                if (productBundle.ProductPages != null)
                {
                    IEnumerable<ProductPages> _productPages = (productBundle.ProductPages != null) ? GetProductPagesByWebsiteLanguageId(productBundle, websiteLanguage.Id) : null;

                    foreach (ProductPages productPage in _productPages)
                    {
                        Pages _page = new PageClient(_context).GetPageByAlternateGuidAndWebsiteLanguageId(productPage.WebsiteLanguageId, productPage.PageAlternateGuid);

                        Dictionary<string, object> categorie = new Dictionary<string, object>
                        {
                            { "id", _page.AlternateGuid },
                            { "text", _page.Name }
                        };
                        categories.Add(categorie);
                    }
                }

                Dictionary<string, object> child = new Dictionary<string, object>
                {
                    { "id", websiteLanguage.Id },
                    { "code", productBundle.Languages.FirstOrDefault(Languages => Languages.Id == websiteLanguage.LanguageId).Code },
                    { "current", (websiteLanguage.Id == websiteLanguageId) ? true: false },
                    { "categories", categories }
                };
                languages.Add(child);
            }

            return languages;
        }

        public Dictionary<string, object> ConvertProductBundleToJson(ProductBundle productBundle, int websiteLanguageId, int websiteId)
        {
            return new Dictionary<string, object>
            {
                { "item", (productBundle.Product != null) ? productBundle.Product : new Products() { Active = false, ProductTemplateId = productBundle.ProductTemplate.Id } },
                { "uploads", productBundle.ProductUploads },
                { "resources", ConvertProductResourcesToJson(productBundle, websiteLanguageId) },
                { "settings", new SettingClient(_context).ConvertEcommerceSettingsToJson(productBundle.ProductTemplate, websiteId) },
                { "categories", ConvertProductPagesToJson(productBundle, websiteLanguageId) },
                { "url", new Website(_context).GetWebsiteUrl(websiteLanguageId) }
            };
        }

        public List<Dictionary<string, object>> ConvertProductBundlesToJson(List<ProductBundle> productBundles, int websiteLanguageId, int websiteId)
        {
            SettingClient settingClient = new SettingClient(_context);
            Commerce commerce = new Commerce(_context);
            commerce.SetPriceFormatVariables(websiteId);
            int digitsAfterDecimal = Int32.Parse(settingClient.GetSettingValueByKey("digitsAfterDecimal", "website", websiteId));
            string currency = settingClient.GetSettingValueByKey("currency", "website", websiteId);
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();
            foreach (ProductBundle productBundle in productBundles)
            {
                bool promo = commerce.IsPromoEnabled(productBundle.Product.PromoSchedule ?? false, productBundle.Product.PromoFromDate, productBundle.Product.PromoToDate, productBundle.Product.PromoPrice);

                Dictionary<string, object> item = new Dictionary<string, object>
                {
                    { "id", productBundle.Product.Id },
                    { "sku", productBundle.Product.Sku },
                    { "price", commerce.GetPriceFormat(decimal.Round(promo ? productBundle.Product.PromoPrice : productBundle.Product.Price, digitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(), currency) },
                    { "title", productBundle.Product.Name },
                    { "categories", CombineProductPagesToString(productBundle.ProductPages.Where(ProductPage => ProductPage.WebsiteLanguageId == websiteLanguageId)) }
                };

                items.Add(item);
            }

            return items;
        }

        public List<Dictionary<string, object>> ConvertProductFilesToJson(IEnumerable<ProductFiles> productFiles, int websiteId)
        {
            Websites _website = new Website(_context).GetWebsiteById(websiteId);
            string path = $@"/websites/{_website.Folder}";

            List<Dictionary<string, object>> files = new List<Dictionary<string, object>>();
            foreach (ProductFiles productFile in productFiles)
            {
                Dictionary<string, object> child = new Dictionary<string, object>
                {
                    { "id", productFile.Id},
                    { "originalPath", path + productFile.OriginalPath.Replace('\\', '/').Replace("~/", "/")},
                    { "compressedPath", path + productFile.CompressedPath.Replace('\\', '/').Replace("~/", "/")},
                    { "alt", productFile.Alt},
                    { "customOrder", productFile.CustomOrder},
                    { "active", productFile.Active}
                };
                files.Add(child);
            }

            return files;
        }

        public List<Dictionary<string, object>> GetProductOptionsJson(int websiteId, int websiteLanguageId)
        {
            return ConvertProductsToJson(GetProductBundles(websiteId), websiteLanguageId);
        }

        public List<Dictionary<string, object>> ConvertProductsToJson(List<ProductBundle> productBundles, int websiteLanguageId)
        {
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();

            foreach (ProductBundle productBundle in productBundles)
            {
                items.Add(new Dictionary<string, object>
                {
                    { "id", productBundle.Product.Id },
                    { "text", productBundle.Product.Name }
                });
            }

            return items;
        }

        public Dictionary<string, object> ConvertProductOptionsToJson(IQueryable<Products> products)
        {
            List<Dictionary<string, object>> dic = new List<Dictionary<string, object>>();
            foreach (Products product in products)
            {
                Dictionary<string, object> item = new Dictionary<string, object>
                {
                    { "id", product.Id },
                    { "text", product.Name }
                };

                dic.Add(item);
            }

            return new Dictionary<string, object> {
                { "results", dic },
                { "pagination", new Dictionary<string, object> { { "more", false } } }
            };
        }

        public string CombineProductPagesToString(IEnumerable<ProductPages> productPages)
        {
            List<string> list = new List<string>();
            foreach (ProductPages productPage in productPages)
            {
                Pages _page = new PageClient(_context).GetPageByAlternateGuidAndWebsiteLanguageId(productPage.WebsiteLanguageId, productPage.PageAlternateGuid);
                if (_page != null) { list.Add(_page.Name); }
            }

            string items = string.Join(",", list);

            return items.Length > 80 ? items.Substring(0, 80) + "..." : items;
        }

        public Dictionary<string, object> GetProductOptionsArrayList(int websiteId, string name)
        {
            return ConvertProductOptionsToJson(GetProductsByWebsiteIdAndActiveAndNameAndOrderByName(websiteId, true, name));
        }

        public string ValidateBackorder(string value)
        {
            List<string> validTargets = new List<string>() {
                "no", "notify", "allow"
            };

            return (validTargets.Any(value.Contains)) ? value : "no";
        }

        public string ValidateStockStatus(string value)
        {
            List<string> validTargets = new List<string>() {
                "stock", "out", "backorder"
            };

            return (validTargets.Any(value.Contains)) ? value : "stock";
        }

        public string ValidateTaxStatus(string value)
        {
            List<string> validTargets = new List<string>() {
                "taxable"//, "shipping", "no"
            };

            return (validTargets.Any(value.Contains)) ? value : "taxable";
        }
    }
}