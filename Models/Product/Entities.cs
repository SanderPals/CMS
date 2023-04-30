using Site.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Site.Models.Product
{
    public partial class ProductClient
    {
        public ProductTemplates GetProductTemplateByWebsiteId(int websiteId)
        {
            return _context.ProductTemplates.FirstOrDefault(productTemplate => productTemplate.WebsiteId == websiteId);
        }

        public ProductTemplates GetProductTemplateById(int productTemplateId)
        {
            return _context.ProductTemplates.FirstOrDefault(productTemplate => productTemplate.Id == productTemplateId);
        }

        public ProductBundle GetProductAndProductTemplate(int id)
        {
            return _context.Products.Join(_context.ProductTemplates, Products => Products.ProductTemplateId, ProductTemplates => ProductTemplates.Id, (Products, ProductTemplates) => new { Products, ProductTemplates })
                                    .Select(x => new ProductBundle()
                                    {
                                        Product = x.Products,
                                        ProductTemplate = x.ProductTemplates
                                    })
                                    .FirstOrDefault(x => x.Product.Id == id);
        }

        public List<ProductBundle> GetProductBundles(int websiteId)
        {
            return _context.Products.Join(_context.ProductTemplates, Products => Products.ProductTemplateId, ProductTemplates => ProductTemplates.Id, (Products, ProductTemplates) => new { Products, ProductTemplates })
                                    .GroupJoin(_context.WebsiteLanguages.Join(_context.Languages, WebsiteLanguages => WebsiteLanguages.LanguageId, Languages => Languages.Id, (WebsiteLanguages, Languages) => new { WebsiteLanguages, Languages }), x => x.ProductTemplates.WebsiteId, WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.WebsiteLanguages.WebsiteId, (x, WebsiteLanguagesAndLanguages) => new { x.Products, x.ProductTemplates, WebsiteLanguagesAndLanguages })
                                    .GroupJoin(_context.ProductPages, x => x.Products.Id, ProductPages => ProductPages.ProductId, (x, ProductPages) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, ProductPages })
                                    .GroupJoin(_context.ProductPageSettings, x => x.Products.Id, ProductPageSettings => ProductPageSettings.ProductId, (x, ProductPageSettings) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, x.ProductPages, ProductPageSettings })
                                    .GroupJoin(_context.ProductFields.OrderBy(ProductFields => ProductFields.CustomOrder), x => x.ProductTemplates.Id, ProductFields => ProductFields.ProductTemplateId, (x, ProductFields) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, x.ProductPages, x.ProductPageSettings, ProductFields })
                                    .GroupJoin(_context.ProductResources, x => x.Products.Id, ProductResources => ProductResources.ProductId, (x, ProductResources) => new { x.Products, x.ProductTemplates, x.ProductPages, x.WebsiteLanguagesAndLanguages, x.ProductPageSettings, x.ProductFields, ProductResources })
                                    .GroupJoin(_context.ProductUploads.OrderBy(ProductUploads => ProductUploads.CustomOrder), x => x.ProductTemplates.Id, ProductUploads => ProductUploads.ProductTemplateId, (x, ProductUploads) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, x.ProductPages, x.ProductPageSettings, x.ProductFields, x.ProductResources, ProductUploads })
                                    .Select(x => new ProductBundle()
                                    {
                                        Languages = x.WebsiteLanguagesAndLanguages.Select(WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.Languages),
                                        ProductFields = x.ProductFields,
                                        ProductPages = x.ProductPages,
                                        ProductPageSettings = x.ProductPageSettings,
                                        ProductResources = x.ProductResources,
                                        Product = x.Products,
                                        ProductTemplate = x.ProductTemplates,
                                        ProductUploads = x.ProductUploads,
                                        WebsiteLanguages = x.WebsiteLanguagesAndLanguages.Select(WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.WebsiteLanguages)
                                    })
                                    .Where(ProductBundle => ProductBundle.ProductTemplate.WebsiteId == websiteId)
                                    .ToList();
        }

        public ProductBundle GetProductBundleByWebsiteId(int websiteId)
        {
            return _context.ProductTemplates.GroupJoin(_context.WebsiteLanguages.Join(_context.Languages, WebsiteLanguages => WebsiteLanguages.LanguageId, Languages => Languages.Id, (WebsiteLanguages, Languages) => new { WebsiteLanguages, Languages }), ProductTemplates => ProductTemplates.WebsiteId, WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.WebsiteLanguages.WebsiteId, (ProductTemplates, WebsiteLanguagesAndLanguages) => new { ProductTemplates, WebsiteLanguagesAndLanguages })
                                            .GroupJoin(_context.ProductFields.OrderBy(ProductFields => ProductFields.CustomOrder), x => x.ProductTemplates.Id, ProductFields => ProductFields.ProductTemplateId, (x, ProductFields) => new { x.ProductTemplates, x.WebsiteLanguagesAndLanguages, ProductFields })
                                            .GroupJoin(_context.ProductUploads.OrderBy(ProductUploads => ProductUploads.CustomOrder), x => x.ProductTemplates.Id, ProductUploads => ProductUploads.ProductTemplateId, (x, ProductUploads) => new { x.ProductTemplates, x.WebsiteLanguagesAndLanguages, x.ProductFields, ProductUploads })
                                            .Select(x => new ProductBundle()
                                            {
                                                Languages = x.WebsiteLanguagesAndLanguages.Select(WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.Languages),
                                                ProductFields = x.ProductFields,
                                                ProductTemplate = x.ProductTemplates,
                                                ProductUploads = x.ProductUploads,
                                                WebsiteLanguages = x.WebsiteLanguagesAndLanguages.Select(WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.WebsiteLanguages)
                                            })
                                            .FirstOrDefault(ProductBundle => ProductBundle.ProductTemplate.WebsiteId == websiteId);
        }

        public ProductBundle GetProductBundleByProductId(int productId, int websiteId)
        {
            return _context.Products.Join(_context.ProductTemplates, Products => Products.ProductTemplateId, ProductTemplates => ProductTemplates.Id, (Products, ProductTemplates) => new { Products, ProductTemplates })
                                    .GroupJoin(_context.WebsiteLanguages.Join(_context.Languages, WebsiteLanguages => WebsiteLanguages.LanguageId, Languages => Languages.Id, (WebsiteLanguages, Languages) => new { WebsiteLanguages, Languages }), x => x.ProductTemplates.WebsiteId, WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.WebsiteLanguages.WebsiteId, (x, WebsiteLanguagesAndLanguages) => new { x.Products, x.ProductTemplates, WebsiteLanguagesAndLanguages })
                                    .GroupJoin(_context.ProductPages, x => x.Products.Id, ProductPages => ProductPages.ProductId, (x, ProductPages) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, ProductPages })
                                    .GroupJoin(_context.ProductPageSettings, x => x.Products.Id, ProductPageSettings => ProductPageSettings.ProductId, (x, ProductPageSettings) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, x.ProductPages, ProductPageSettings })
                                    .GroupJoin(_context.ProductResources, x => x.Products.Id, ProductResources => ProductResources.ProductId, (x, ProductResources) => new { x.Products, x.ProductTemplates, x.ProductPages, x.WebsiteLanguagesAndLanguages, x.ProductPageSettings, ProductResources })
                                    .GroupJoin(_context.ProductFields.OrderBy(ProductFields => ProductFields.CustomOrder), x => x.ProductTemplates.Id, ProductFields => ProductFields.ProductTemplateId, (x, ProductFields) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, x.ProductPages, x.ProductPageSettings, x.ProductResources, ProductFields })
                                    .GroupJoin(_context.ProductUploads.OrderBy(ProductUploads => ProductUploads.CustomOrder), x => x.ProductTemplates.Id, ProductUploads => ProductUploads.ProductTemplateId, (x, ProductUploads) => new { x.Products, x.ProductTemplates, x.WebsiteLanguagesAndLanguages, x.ProductPages, x.ProductPageSettings, x.ProductFields, x.ProductResources, ProductUploads })
                                    .Select(x => new ProductBundle()
                                    {
                                        Languages = x.WebsiteLanguagesAndLanguages.Select(WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.Languages),
                                        ProductFields = x.ProductFields,
                                        ProductPages = x.ProductPages,
                                        ProductPageSettings = x.ProductPageSettings,
                                        ProductResources = x.ProductResources,
                                        Product = x.Products,
                                        ProductTemplate = x.ProductTemplates,
                                        ProductUploads = x.ProductUploads,
                                        WebsiteLanguages = x.WebsiteLanguagesAndLanguages.Select(WebsiteLanguagesAndLanguages => WebsiteLanguagesAndLanguages.WebsiteLanguages)
                                    })
                                    .FirstOrDefault(ProductBundle => ProductBundle.Product.Id == productId && ProductBundle.ProductTemplate.WebsiteId == websiteId);
        }

        public IQueryable<Products> GetProductsByWebsiteIdAndActiveAndNameAndOrderByName(int websiteId, bool active, string name)
        {
            return _context.Products.Join(_context.ProductTemplates, Product => Product.ProductTemplateId, ProductTemplate => ProductTemplate.Id, (Product, ProductTemplate) => new { Product, ProductTemplate })
                                    .Where(x => x.ProductTemplate.WebsiteId == websiteId)
                                    .Select(x => x.Product)
                                    .Where(Product => Product.Active == active && Product.Name.ToLower().Contains(name.ToLower()))
                                    .OrderBy(Product => Product.Name);
        }

        public Products GetProductById(int id)
        {
            return _context.Products.FirstOrDefault(Product => Product.Id == id);
        }

        public Products GetProductByIdAndWebsiteId(int id, int websiteId)
        {
            return _context.ProductTemplates.Where(ProductTemplate => ProductTemplate.WebsiteId == websiteId)
                                            .Join(_context.Products, ProductTemplate => ProductTemplate.Id, Product => Product.ProductTemplateId, (ProductTemplate, Product) => new { ProductTemplate, Product })
                                            .Select(x => x.Product)
                                            .FirstOrDefault(Product => Product.Id == id);
        }

        public ProductResources GetProductResourcesByProductFieldIdAndWebsiteLanguageId(ProductBundle productBundle, int productFieldId, int websiteLanguageId)
        {
            return productBundle.WebsiteLanguages.Where(WebsiteLanguages => WebsiteLanguages.Id == websiteLanguageId)
                                                 .Join(productBundle.ProductResources, WebsiteLanguages => WebsiteLanguages.Id, ProductResources => (ProductResources != null) ? ProductResources.WebsiteLanguageId : 0, (WebsiteLanguages, ProductResources) => new { WebsiteLanguages, ProductResources })
                                                 .Select(x => x.ProductResources)
                                                 .FirstOrDefault(ProductResources => ProductResources.ProductFieldId == productFieldId);
        }

        public IEnumerable<ProductPages> GetProductPagesByWebsiteLanguageId(ProductBundle productBundle, int websiteLanguageId)
        {
            return productBundle.WebsiteLanguages.Where(WebsiteLanguages => WebsiteLanguages.Id == websiteLanguageId)
                                                 .Join(productBundle.ProductPages, WebsiteLanguages => WebsiteLanguages.Id, ProductPages => (ProductPages != null) ? ProductPages.WebsiteLanguageId : 0, (WebsiteLanguages, ProductPages) => new { WebsiteLanguages, ProductPages })
                                                 .Select(x => x.ProductPages);
        }

        public ProductResources GetProductResourceByCallNameAndWebsiteLanguageId(ProductBundle productBundle, string callName, int websiteLanguageId)
        {
            return productBundle.ProductFields.Where(ProductField => ProductField.CallName == "title")
                                              .Join(productBundle.ProductResources, ProductField => ProductField.Id, ProductResource => ProductResource.ProductFieldId, (ProductField, ProductResource) => new { ProductField, ProductResource })
                                              .Select(x => x.ProductResource)
                                              .FirstOrDefault(ProductResource => ProductResource.WebsiteLanguageId == websiteLanguageId);
        }

        public IEnumerable<ProductFiles> GetProductFilesByProductUploadIdAndProductId(int productUploadId, int productId)
        {
            return _context.ProductFiles.Where(ProductFile => ProductFile.ProductUploadId == productUploadId && ProductFile.ProductId == productId)
                                        .OrderBy(ProductFile => ProductFile.CustomOrder);
        }

        public Products UpdateProductActive(Products products)
        {
            Products _product = new Products { Id = products.Id, Active = products.Active };
            _context.Products.Attach(_product);
            _context.Entry(_product).Property(Product => Product.Active).IsModified = true;
            _context.SaveChanges();

            return _product;
        }

        public void InsertProductFile(int productId, int productUploadId, string originalPath, string compressedPath, string alt, int customOrder, bool active)
        {
            ProductFiles _productFile = new ProductFiles { ProductId = productId, ProductUploadId = productUploadId, OriginalPath = originalPath, CompressedPath = compressedPath, Alt = alt, CustomOrder = customOrder, Active = active };
            _context.ProductFiles.Add(_productFile);
            _context.SaveChanges();
        }

        public void UpdateProductResourceText(int id, string text)
        {
            ProductResources _productResource = _context.ProductResources.FirstOrDefault(ProductResource => ProductResource.Id == id);
            ProductResources _productResourcePrevious = _productResource;
            _productResource.Text = text;
            _context.Entry(_productResourcePrevious).CurrentValues.SetValues(_productResource);
        }

        public void UpdateProductPageSetting(ProductPageSettings productPageSetting)
        {
            ProductPageSettings _productPageSettingsPrevious = _context.ProductPageSettings.FirstOrDefault(ProductPageSetting => ProductPageSetting.Id == productPageSetting.Id);
            _context.Entry(_productPageSettingsPrevious).CurrentValues.SetValues(productPageSetting);
        }

        public Products InsertProduct(bool active, string backorders, int downloadExpire, int downloadLimit, decimal height, decimal length, bool manageStock, int maxPerOrder, decimal price, int productTemplateId, DateTime promoFromDate, decimal promoPrice, bool promoSchedule, DateTime promoToDate, bool reviews, int shippingClassId, string sku, int stockQuantity, string stockStatus, int taxClassId, string taxStatus, decimal weight, decimal width, string name, int customOrder)
        {
            Products _product = new Products { Active = active, Backorders = backorders, DownloadExpire = downloadExpire, DownloadLimit = downloadLimit, Height = height, Length = length, ManageStock = manageStock, MaxPerOrder = maxPerOrder, Price = price, ProductTemplateId = productTemplateId, PromoFromDate = promoFromDate, PromoPrice = promoPrice, PromoSchedule = promoSchedule, PromoToDate = promoToDate, Reviews = reviews, ShippingClassId = shippingClassId, Sku = sku, StockQuantity = stockQuantity, StockStatus = stockStatus, TaxClassId = taxClassId, TaxStatus = taxStatus, Weight = weight, Width = width, Name = name, CustomOrder = customOrder };
            _context.Products.Add(_product);
            _context.SaveChanges();

            return _product;
        }

        public bool UpdateProduct(Products product)
        {
            product.ProductFiles = null;
            product.ProductPages = null;
            product.ProductPageSettings = null;
            product.ProductResources = null;
            product.ProductTemplate = null;

            _context.Products.Update(product);
            _context.SaveChanges();

            return true;
        }
    }
}
