using Site.Data;
using System.Collections.Generic;

namespace Site.Models.Product
{
    public partial class ProductClient
    {
        public class ProductBundle
        {
            public IEnumerable<Languages> Languages { get; set; }
            public ProductFields ProductField { get; set; }
            public IEnumerable<ProductFields> ProductFields { get; set; }
            public IEnumerable<ProductPages> ProductPages { get; set; }
            public IEnumerable<ProductPageSettings> ProductPageSettings { get; set; }
            public ProductResources ProductResource { get; set; }
            public IEnumerable<ProductResources> ProductResources { get; set; }
            public Products Product { get; set; }
            public IEnumerable<Products> Products { get; set; }
            public ProductTemplates ProductTemplate { get; set; }
            public IEnumerable<ProductUploads> ProductUploads { get; set; }
            public IEnumerable<WebsiteLanguages> WebsiteLanguages { get; set; }
        }

        public class CategorieUpdate
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public bool Current { get; set; }
            public object Categories { get; set; }
        }

        public class ProductUpdate
        {
            public Products Item { get; set; }
            public List<CategorieUpdate> Categories { get; set; }
        }

        public class ProductResourceUpdate
        {
            public int Id { get; set; }
            public int FieldId { get; set; }
            public object Text { get; set; }
            public string Heading { get; set; }
            public string Type { get; set; }
        }

        public class ProductLanguageUpdate
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public bool Current { get; set; }
            public List<ProductResourceUpdate> Fields { get; set; }
            public ProductPageSettings Page { get; set; }
        }

        public class ProductDataUpdate
        {
            public int Id { get; set; }
            public List<ProductLanguageUpdate> Resources { get; set; }
        }
    }
}
