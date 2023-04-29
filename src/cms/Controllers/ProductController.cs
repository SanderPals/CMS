using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Models;
using Site.Models.Product;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using static Site.Models.Product.ProductClient;
using System.Threading.Tasks;

namespace Site.Controllers
{
    public class ProductController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<ProductController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public ProductController(SiteContext context,
            IStringLocalizer<ProductController> localizer, UserManager<ApplicationUser> userManager = null, IHostingEnvironment env = null, IHttpContextAccessor httpContextAccessor = null, IHttpContextAccessor contextAccessor = null)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _contextAccessor = contextAccessor;
            _localizer = localizer;

            websiteId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId").Value);
            websiteLanguageId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteLanguageId").Value);
        }

        [Route("/api/product")]
        [HttpGet]
        public IActionResult GetProductApi(int id)
        {
            try
            {
                ProductClient product = new ProductClient(_context);
                return Ok(Json(product.ConvertProductBundleToJson(product.GetProductBundle(id, websiteId), websiteLanguageId, websiteId)));
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    redirect = "/products"
                }));
            }
        }

        [Route("/api/products")]
        [HttpGet]
        public IActionResult GetProductsApi()
        {
            try
            {
                ProductClient product = new ProductClient(_context);
                return Ok(Json(product.ConvertProductBundlesToJson(product.GetProductBundles(websiteId), websiteLanguageId, websiteId)));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowListProducts"].Value
                }));
            }
        }

        [Route("/api/product-resources")]
        [HttpPost]
        public IActionResult PostProductResourcesApiAsync([FromBody] ProductDataUpdate productDataUpdate)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new ProductClient(_context).UpdateProductResourcesAndProductPageSettings(productDataUpdate.Resources, productDataUpdate.Id, websiteId, websiteLanguageId);
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateResources"].Value
                }));
            }
        }

        [Route("/api/product-active")]
        [HttpPost]
        public IActionResult PostProductActiveApi([FromBody]Products product)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                Products _product = new ProductClient(_context).UpdateProductActive(product);

                return Ok(Json(new
                {
                    messageType = "success",
                    message = (_product.Active ?? false) 
                    ? _localizer["ProductActivated"].Value
                    : _localizer["ProductDeactivated"].Value
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateProductStatus"].Value
                }));
            }
        }

        [Route("/api/product-file")]
        [HttpPost]
        public IActionResult PostProductFileApi()
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new ProductClient(_context, _userManager, _env).InsertFile(Request, websiteId);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUploadFile"].Value
                }));
            }
        }

        [Route("/api/product")]
        [HttpPost]
        public IActionResult PostProductApi([FromBody] ProductUpdate productUpdate)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            int id = productUpdate.Item.Id;
            try
            {
                return new ProductClient(_context).UpdateOrInsert(productUpdate, id);
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = (id == 0) 
                    ? _localizer["CannotAddProduct"].Value
                    : _localizer["CannotUpdateProduct"].Value
                }));
            }
        }

        [Route("/api/delete-product-file")]
        [HttpPost]
        public IActionResult DeleteProductFileApi(int id)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new ProductClient(_context, null, _env).DeleteProductFile(id, websiteId);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotDeleteFile"].Value
                }));
            }
        }

        [Route("/api/delete-product")]
        [HttpPost]
        public IActionResult DeleteProductApi(int id)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new ProductClient(_context, null, _env).DeleteProduct(id, websiteId);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotDeleteProduct"].Value
                }));
            }
        }

        [Route("/api/product-files-custom-order")]
        [HttpPost]
        public IActionResult PostProductFilesOrderApi([FromBody] List<ProductFiles> productFiles)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new ProductClient(_context).UpdateProductFilesCustomOrder(productFiles);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateOrderFiles"].Value
                }));
            }
        }

        [Route("/api/product-file-active")]
        [HttpPost]
        public IActionResult PostProductFileActiveApi([FromBody]ProductFiles productFile)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new ProductClient(_context).UpdateProductFileActive(productFile);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateFileStatus"].Value
                }));
            }
        }

        [Route("/api/product-file-alt")]
        [HttpPost]
        public IActionResult PostProductFileAltApi([FromBody]ProductFiles productFile)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new ProductClient(_context).UpdateProductFileAlt(productFile);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateFileDescription"].Value
                }));
            }
        }

        [Route("/api/product-files")]
        [HttpGet]
        public IActionResult PostProductFilesApi(int id, int uploadId)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                ProductClient product = new ProductClient(_context);
                return Ok(Json(product.ConvertProductFilesToJson(product.GetProductFilesByProductUploadIdAndProductId(uploadId, id), websiteId)));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotGetFiles"].Value
                }));
            }
        }

        [Route("/api/product-options")]
        [HttpGet]
        public IActionResult GetOptions()
        {
            try
            {
                return Ok(Json(new ProductClient(_context).GetProductOptionsJson(websiteId, websiteLanguageId)));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowProductOptions"].Value
                }));
            }
        }

        [Route("/api/products-by-name")]
        [HttpGet]
        public IActionResult GetProductsByNameApi(string term)
        {
            try
            {
                return Ok(Json(new ProductClient(_context).GetProductOptionsArrayList(websiteId, term)));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowProducts"].Value
                }));
            }
        }

        [Route("/products")]
        public IActionResult Index()
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { LocalRedirect("/dashboard"); }

            return View();            
        }

        [Route("/products/add")]
        [Route("/products/edit/{id}")]
        public IActionResult Add()
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { LocalRedirect("/dashboard"); }

            return View();
        }
    }
}