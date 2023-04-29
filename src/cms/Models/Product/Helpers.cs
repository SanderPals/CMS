using Site.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Site.Models.Product
{
    public partial class ProductClient
    {
        public ProductBundle GetProductBundle(int productId, int websiteId)
        {
            //Product product = new Product(_context);
            if (productId != 0)
            {
                return GetProductBundleByProductId(productId, websiteId);
            }
            else
            {
                return GetProductBundleByWebsiteId(websiteId);
            }
        }
    }
}
