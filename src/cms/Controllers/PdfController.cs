using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using Site.Data;
using Site.Models;
using Site.Models.Setting;
using Site.Models.ViewModels;
using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Localization;
using static Site.Models.Order;

namespace Site.Controllers
{
    public class PdfController : Controller
    {
        private readonly SiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDataProtectionProvider _provider;
        private IStringLocalizer<PdfController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public PdfController(SiteContext context, UserManager<ApplicationUser> userManager , IHostingEnvironment env, IHttpContextAccessor httpContextAccessor, 
            IStringLocalizer<PdfController> localizer, IDataProtectionProvider provider = null)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _provider = provider;
            _localizer = localizer;

            if (new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId") != null)
            {
                websiteId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId").Value);
                websiteLanguageId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteLanguageId").Value);
            }
        }

        [HttpGet]
        [Route("invoice/{id}")]
        public ViewAsPdf Index()
        {
            int orderId = Int32.Parse(RouteData.Values["id"].ToString());

            SettingClient setting = new SettingClient(_context);

            Order order = new Order(_context);
            OrderBundle orderBundle = order.GetOrderBundleByWebsiteId(orderId, websiteId);

            if (orderBundle != null)
            {
                if (orderBundle.Order.Status.ToLower() == "completed" || orderBundle.Order.Status.ToLower() == "credit")
                {
                    order.Commerce = new Commerce(_context);
                    order.Commerce.SetPriceFormatVariables(websiteId);
                    order.DigitsAfterDecimal = Int32.Parse(setting.GetSettingValueByKey("digitsAfterDecimal", "website", websiteId));
                    order.Currency = setting.GetSettingValueByKey("currency", "website", websiteId);
                    decimal shippingCosts = order.GetTotalShippingCosts(orderBundle.OrderShippingZoneMethods, orderBundle.OrderLines, orderBundle.OrderFees, order.DigitsAfterDecimal);
                    decimal productsTotal = order.GetTotalLinePrice(orderBundle.OrderLines, order.DigitsAfterDecimal);
                    decimal feesTotal = order.GetTotalFeePrice(orderBundle.OrderFees, order.DigitsAfterDecimal);

                    Encryptor encryptor = new Encryptor(_provider);
                    Orders _order = orderBundle.Order;
                    var model = new PdfViewModel
                    {
                        Website = new Website(_context).GetCleanWebsiteUrlByWebsiteId(websiteId),
                        AddressLine1 = setting.GetSettingValueByKey("addressLine1", "website", websiteId),
                        ZipCode = setting.GetSettingValueByKey("zipCode", "website", websiteId),
                        Vat = setting.GetSettingValueByKey("vat", "website", websiteId),
                        Coc = setting.GetSettingValueByKey("coc", "website", websiteId),
                        Country = setting.GetSettingValueByKey("country", "website", websiteId),
                        City = setting.GetSettingValueByKey("city", "website", websiteId),
                        Date = _order.CreatedDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                        OrderNumber = _order.OrderNumber,
                        InvoiceNumber = _order.InvoiceNumber,
                        RefInvoiceNumber = _order.RefInvoiceNumber,
                        OrderLines = orderBundle.OrderLines,
                        OrderFees = orderBundle.OrderFees,
                        ShippingAddressLine1 = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingAddressLine1) : encryptor.Decrypt(_order.BillingAddressLine1)),
                        ShippingAddressLine2 = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine2)) ? encryptor.Decrypt(_order.ShippingAddressLine2) : encryptor.Decrypt(_order.BillingAddressLine2)),
                        ShippingCity = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingCity) : encryptor.Decrypt(_order.BillingCity)),
                        ShippingCompany = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingCompany) : encryptor.Decrypt(_order.BillingCompany)),
                        ShippingCountry = "Nederland",//(!string.IsNullOrWhiteSpace(_order.ShippingAddressLine1) ? _order.ShippingCountry : _order.BillingCountry),
                        ShippingName = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingFirstName) + " " + encryptor.Decrypt(_order.ShippingLastName) : encryptor.Decrypt(_order.BillingFirstName) + " " + encryptor.Decrypt(_order.BillingLastName)),
                        ShippingZipCode = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingZipCode) : encryptor.Decrypt(_order.BillingZipCode)),
                        ShippingState = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingState)) ? encryptor.Decrypt(_order.ShippingState) : encryptor.Decrypt(_order.BillingState)),
                        BillingAddressLine1 = encryptor.Decrypt(_order.BillingAddressLine1),
                        BillingAddressLine2 = encryptor.Decrypt(_order.BillingAddressLine2),
                        BillingCity = encryptor.Decrypt(_order.BillingCity),
                        BillingCompany = encryptor.Decrypt(_order.BillingCompany),
                        BillingCountry = "Nederland",
                        BillingZipCode = encryptor.Decrypt(_order.BillingZipCode),
                        BillingEmail = encryptor.Decrypt(_order.BillingEmail),
                        BillingName = encryptor.Decrypt(_order.BillingFirstName) + " " + encryptor.Decrypt(_order.BillingLastName),
                        BillingPhoneNumber = encryptor.Decrypt(_order.BillingPhoneNumber),
                        BillingVatNumber = encryptor.Decrypt(_order.BillingVatNumber),
                        BillingState = encryptor.Decrypt(_order.BillingState),
                        TaxClasses = order.TaxClasses,
                        Status = _order.Status.ToLower(),
                        Note = _order.Note,
                        ShippingCosts = shippingCosts,
                        Subtotal = order.Commerce.GetPriceFormat(decimal.Round((productsTotal) + feesTotal, order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                        Tax = order.Commerce.GetPriceFormat(decimal.Round(order.Tax, order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                        TotalExclusive = order.Commerce.GetPriceFormat(decimal.Round((((shippingCosts + productsTotal) + feesTotal) - order.Tax), order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                        Total = order.Commerce.GetPriceFormat(decimal.Round(((shippingCosts + productsTotal) + feesTotal), order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                        PriceFormat = order.Commerce.GetPriceFormat("price", order.Currency),
                        DigitsFormat = "0.".PadRight(order.DigitsAfterDecimal + "0.".Length, '0'),
                        DigitsAfterDecimal = order.DigitsAfterDecimal
                    };

                    var filePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath + $@"\Files\Pdf", "Invoice_" + _order.Id + ".pdf"));

                    ////Create directory is it does not exist
                    //if (!Directory.Exists(_env.ContentRootPath + $@"\Files\Pdf"))
                    //{
                    //    Directory.CreateDirectory(_env.ContentRootPath + $@"\Files\Pdf");
                    //}

                    RouteValueDictionary routeValueDictionary = new RouteValueDictionary()
                    {
                        { "id", orderId },
                        { "websiteId", websiteId }
                    };

                    string cusomtSwitches = string.Format("--print-media-type --header-spacing 2 --header-html {1} --footer-html {0}", Url.Action("Footer", "Pdf", routeValueDictionary, "https"), Url.Action("Master", "Pdf", routeValueDictionary, "https"));

                    return new ViewAsPdf("Invoice", model)
                    {
                        FileName = new Models.File().RemoveInvalidCharacters("Factuur " + _order.OrderNumber + ".pdf"),
                        CustomSwitches = cusomtSwitches,
                        PageSize = Size.A4,
                        PageOrientation = Orientation.Portrait,
                        PageMargins = { Top = 109, Right = 0, Bottom = 30, Left = 0 }
                        //SaveOnServerPath = filePath
                    };
                }
            }

            return null;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("spine-api/create-master")]
        public ActionResult Master(int id, int websiteId)
        {
            int orderId = id;

            SettingClient setting = new SettingClient(_context);

            Order order = new Order(_context);
            OrderBundle orderBundle = order.GetOrderBundleByWebsiteId(orderId, websiteId);
            order.Commerce = new Commerce(_context);
            order.Commerce.SetPriceFormatVariables(websiteId);
            order.DigitsAfterDecimal = Int32.Parse(setting.GetSettingValueByKey("digitsAfterDecimal", "website", websiteId));
            order.Currency = setting.GetSettingValueByKey("currency", "website", websiteId);
            decimal shippingCosts = order.GetTotalShippingCosts(orderBundle.OrderShippingZoneMethods, orderBundle.OrderLines, orderBundle.OrderFees, order.DigitsAfterDecimal);
            decimal productsTotal = order.GetTotalLinePrice(orderBundle.OrderLines, order.DigitsAfterDecimal);
            decimal feesTotal = order.GetTotalFeePrice(orderBundle.OrderFees, order.DigitsAfterDecimal);

            Encryptor encryptor = new Encryptor(_provider);
            Orders _order = orderBundle.Order;
            var model = new PdfViewModel
            {
                Website = new Website(_context).GetCleanWebsiteUrlByWebsiteId(websiteId),
                AddressLine1 = setting.GetSettingValueByKey("addressLine1", "website", websiteId),
                ZipCode = setting.GetSettingValueByKey("zipCode", "website", websiteId),
                Vat = setting.GetSettingValueByKey("vat", "website", websiteId),
                Coc = setting.GetSettingValueByKey("coc", "website", websiteId),
                Country = setting.GetSettingValueByKey("country", "website", websiteId),
                City = setting.GetSettingValueByKey("city", "website", websiteId),
                Date = _order.CreatedDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                OrderNumber = _order.OrderNumber,
                InvoiceNumber = _order.InvoiceNumber,
                RefInvoiceNumber = _order.RefInvoiceNumber,
                OrderLines = orderBundle.OrderLines,
                OrderFees = orderBundle.OrderFees,
                ShippingAddressLine1 = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingAddressLine1) : encryptor.Decrypt(_order.BillingAddressLine1)),
                ShippingAddressLine2 = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine2)) ? encryptor.Decrypt(_order.ShippingAddressLine2) : encryptor.Decrypt(_order.BillingAddressLine2)),
                ShippingCity = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingCity) : encryptor.Decrypt(_order.BillingCity)),
                ShippingCompany = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingCompany) : encryptor.Decrypt(_order.BillingCompany)),
                ShippingCountry = "Nederland",//(!string.IsNullOrWhiteSpace(_order.ShippingAddressLine1) ? _order.ShippingCountry : _order.BillingCountry),
                ShippingName = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingFirstName) + " " + encryptor.Decrypt(_order.ShippingLastName) : encryptor.Decrypt(_order.BillingFirstName) + " " + encryptor.Decrypt(_order.BillingLastName)),
                ShippingZipCode = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingZipCode) : encryptor.Decrypt(_order.BillingZipCode)),
                ShippingState = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingState)) ? encryptor.Decrypt(_order.ShippingState) : encryptor.Decrypt(_order.BillingState)),
                BillingAddressLine1 = encryptor.Decrypt(_order.BillingAddressLine1),
                BillingAddressLine2 = encryptor.Decrypt(_order.BillingAddressLine2),
                BillingCity = encryptor.Decrypt(_order.BillingCity),
                BillingCompany = encryptor.Decrypt(_order.BillingCompany),
                BillingCountry = "Nederland",
                BillingZipCode = encryptor.Decrypt(_order.BillingZipCode),
                BillingEmail = encryptor.Decrypt(_order.BillingEmail),
                BillingName = encryptor.Decrypt(_order.BillingFirstName) + " " + encryptor.Decrypt(_order.BillingLastName),
                BillingPhoneNumber = encryptor.Decrypt(_order.BillingPhoneNumber),
                BillingVatNumber = encryptor.Decrypt(_order.BillingVatNumber),
                BillingState = encryptor.Decrypt(_order.BillingState),
                Status = _order.Status.ToLower(),
                Note = _order.Note,
                ShippingCosts = shippingCosts,
                Subtotal = order.Commerce.GetPriceFormat(decimal.Round((productsTotal) + feesTotal, order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                Tax = order.Commerce.GetPriceFormat(decimal.Round(order.Tax, order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                TotalExclusive = order.Commerce.GetPriceFormat(decimal.Round((((shippingCosts + productsTotal) + feesTotal) - order.Tax), order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                Total = order.Commerce.GetPriceFormat(decimal.Round(((shippingCosts + productsTotal) + feesTotal), order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                PriceFormat = order.Commerce.GetPriceFormat("price", order.Currency),
                DigitsFormat = "0.".PadRight(order.DigitsAfterDecimal + "0.".Length, '0'),
                DigitsAfterDecimal = order.DigitsAfterDecimal
            };

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("spine-api/create-footer")]
        public ActionResult Footer(int id, int websiteId)
        {
            int orderId = id;

            SettingClient setting = new SettingClient(_context);

            Order order = new Order(_context);
            OrderBundle orderBundle = order.GetOrderBundleByWebsiteId(orderId, websiteId);
            order.Commerce = new Commerce(_context);
            order.Commerce.SetPriceFormatVariables(websiteId);
            order.DigitsAfterDecimal = Int32.Parse(setting.GetSettingValueByKey("digitsAfterDecimal", "website", websiteId));
            order.Currency = setting.GetSettingValueByKey("currency", "website", websiteId);
            decimal shippingCosts = order.GetTotalShippingCosts(orderBundle.OrderShippingZoneMethods, orderBundle.OrderLines, orderBundle.OrderFees, order.DigitsAfterDecimal);
            decimal productsTotal = order.GetTotalLinePrice(orderBundle.OrderLines, order.DigitsAfterDecimal);
            decimal feesTotal = order.GetTotalFeePrice(orderBundle.OrderFees, order.DigitsAfterDecimal);

            Encryptor encryptor = new Encryptor(_provider);
            Orders _order = orderBundle.Order;
            var model = new PdfViewModel
            {
                Website = new Website(_context).GetCleanWebsiteUrlByWebsiteId(websiteId),
                AddressLine1 = setting.GetSettingValueByKey("addressLine1", "website", websiteId),
                ZipCode = setting.GetSettingValueByKey("zipCode", "website", websiteId),
                Vat = setting.GetSettingValueByKey("vat", "website", websiteId),
                Coc = setting.GetSettingValueByKey("coc", "website", websiteId),
                Country = setting.GetSettingValueByKey("country", "website", websiteId),
                City = setting.GetSettingValueByKey("city", "website", websiteId),
                Date = _order.CreatedDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                OrderNumber = _order.OrderNumber,
                InvoiceNumber = _order.InvoiceNumber,
                RefInvoiceNumber = _order.RefInvoiceNumber,
                OrderLines = orderBundle.OrderLines,
                OrderFees = orderBundle.OrderFees,
                ShippingAddressLine1 = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingAddressLine1) : encryptor.Decrypt(_order.BillingAddressLine1)),
                ShippingAddressLine2 = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine2)) ? encryptor.Decrypt(_order.ShippingAddressLine2) : encryptor.Decrypt(_order.BillingAddressLine2)),
                ShippingCity = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingCity) : encryptor.Decrypt(_order.BillingCity)),
                ShippingCompany = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingCompany) : encryptor.Decrypt(_order.BillingCompany)),
                ShippingCountry = "Nederland",//(!string.IsNullOrWhiteSpace(_order.ShippingAddressLine1) ? _order.ShippingCountry : _order.BillingCountry),
                ShippingName = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingFirstName) + " " + encryptor.Decrypt(_order.ShippingLastName) : encryptor.Decrypt(_order.BillingFirstName) + " " + encryptor.Decrypt(_order.BillingLastName)),
                ShippingZipCode = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingAddressLine1)) ? encryptor.Decrypt(_order.ShippingZipCode) : encryptor.Decrypt(_order.BillingZipCode)),
                ShippingState = (!string.IsNullOrWhiteSpace(encryptor.Decrypt(_order.ShippingState)) ? encryptor.Decrypt(_order.ShippingState) : encryptor.Decrypt(_order.BillingState)),
                BillingAddressLine1 = encryptor.Decrypt(_order.BillingAddressLine1),
                BillingAddressLine2 = encryptor.Decrypt(_order.BillingAddressLine2),
                BillingCity = encryptor.Decrypt(_order.BillingCity),
                BillingCompany = encryptor.Decrypt(_order.BillingCompany),
                BillingCountry = "Nederland",
                BillingZipCode = encryptor.Decrypt(_order.BillingZipCode),
                BillingEmail = encryptor.Decrypt(_order.BillingEmail),
                BillingName = encryptor.Decrypt(_order.BillingFirstName) + " " + encryptor.Decrypt(_order.BillingLastName),
                BillingPhoneNumber = encryptor.Decrypt(_order.BillingPhoneNumber),
                BillingVatNumber = encryptor.Decrypt(_order.BillingVatNumber),
                BillingState = encryptor.Decrypt(_order.BillingState),
                Status = _order.Status.ToLower(),
                Note = _order.Note,
                ShippingCosts = shippingCosts,
                Subtotal = order.Commerce.GetPriceFormat(decimal.Round((productsTotal) + feesTotal, order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                Tax = order.Commerce.GetPriceFormat(decimal.Round(order.Tax, order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                TotalExclusive = order.Commerce.GetPriceFormat(decimal.Round((((shippingCosts + productsTotal) + feesTotal) - order.Tax), order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                Total = order.Commerce.GetPriceFormat(decimal.Round(((shippingCosts + productsTotal) + feesTotal), order.DigitsAfterDecimal, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("nl-NL").NumberFormat), order.Currency),
                PriceFormat = order.Commerce.GetPriceFormat("price", order.Currency),
                DigitsFormat = "0.".PadRight(order.DigitsAfterDecimal + "0.".Length, '0'),
                DigitsAfterDecimal = order.DigitsAfterDecimal
            };

            return View(model);
        }
    }
}