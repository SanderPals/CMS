using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Models;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization;
using System.Threading.Tasks;

namespace Site.Controllers
{
    public class OrderController : Controller
    {
        private readonly SiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IDataProtectionProvider _provider;
        private IStringLocalizer<OrderController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public OrderController(SiteContext context,
            IStringLocalizer<OrderController> localizer, UserManager<ApplicationUser> userManager = null, IHttpContextAccessor httpContextAccessor = null, IHttpContextAccessor contextAccessor = null, IDataProtectionProvider provider = null)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _contextAccessor = contextAccessor;
            _provider = provider;
            _localizer = localizer;

            websiteId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId").Value);
            websiteLanguageId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteLanguageId").Value);
        }

        [Route("/spine-api/order")]
        [HttpGet]
        public async Task<IActionResult> GetOrderApiAsync(int id)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return Ok(await new Order(_context, _provider).GetOrderAsync(id, websiteId));
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    redirect = "/orders"
                }));
            }
        }

        [Route("/spine-api/order")]
        [HttpPost]
        public async Task<IActionResult> PostOrderApiAsync([FromBody] Orders order)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return await new Order(_context, _provider).UpdateOrInsertOrderAsync(order, order.Id, websiteId);
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = order.Id != 0 
                    ? _localizer["CannotUpdateOrder"].Value
                    : _localizer["CannotCreateOrder"].Value
                }));
            }
        }

        [Route("/spine-api/delete-order")]
        [HttpPost]
        public IActionResult DeleteOrderApi(Orders order)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return Ok(Json(new Order(_context).DeleteOrder(order.Id)));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotDeleteOrder"].Value
                }));
            }
        }

        [Route("/spine-api/orders")]
        [HttpGet]
        public IActionResult GetOrdersApi(int pageNumber, int pageSize)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                Order order = new Order(_context, _provider);
                return order.ConvertOrdersToJson(order.GetOrdersByWebsiteId(websiteId), pageNumber, pageSize);
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowListOrders"].Value
                }));
            }
        }

        [Route("/spine-api/order-line")]
        [HttpPost]
        public IActionResult PostOrderLineApi([FromBody] OrderLines orderLine)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new Order(_context).UpdateOrInsertOrderLine(orderLine, orderLine.Id);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = orderLine.Id != 0 
                    ? _localizer["CannotUpdateOrderLine"].Value
                    : _localizer["CannotAddOrderLine"].Value
                }));
            }
        }

        [Route("/spine-api/order-fee")]
        [HttpPost]
        public IActionResult PostOrderFeeApi([FromBody] OrderFees orderFee)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new Order(_context).UpdateOrInsertOrderFee(orderFee, orderFee.Id);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = orderFee.Id != 0 
                    ? _localizer["CannotUpdateFee"].Value
                    : _localizer["CannotAddFee"].Value
                }));
            }
        }

        [Route("/spine-api/order-shipping")]
        [HttpPost]
        public IActionResult PostOrderShippingZoneMethodApi([FromBody] OrderShippingZoneMethods orderShippingZoneMethod)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new Order(_context).UpdateOrInsertOrderShippingZoneMethod(orderShippingZoneMethod, orderShippingZoneMethod.Id);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = orderShippingZoneMethod.Id != 0 
                    ? _localizer["CannotUpdateShipping"].Value
                    : _localizer["CannotAddShipping"].Value
                }));
            }
        }

        [Route("/spine-api/delete-order-fees-shippings-lines")]
        [HttpPost]
        public IActionResult DeleteOrderFeeApi(Dictionary<string, List<String>> selection)
        {
            if (new SettingClient(_context).GetSettingValueByKey("eCommerce", "website", websiteId).ToLower() != "true") { return StatusCode(400); }

            try
            {
                return new Order(_context).DeleteOrderFeesAndShippingsAndLines(selection);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotDeleteSelection"].Value
                }));
            }
        }

        [Route("/orders")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/orders/add")]
        [Route("/orders/edit/{id}")]
        public async Task<IActionResult> Add()
        {
            if (RouteData.Values["id"] == null)
            {
                Dictionary<string, object> result = await new Order(_context, _provider).GetOrderAsync(0, websiteId);
                int id = (result.FirstOrDefault(x => x.Key.ToLower() == "item").Value as Orders).Id;

                return Redirect("/orders/edit/" + id);
            } 

            return View();
        }
    }
}