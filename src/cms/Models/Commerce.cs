using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Site.Data;
using Site.Models.SendCloud;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using static Site.Models.Order;

namespace Site.Models
{
    public class Commerce : Controller
    {
        private readonly SiteContext _context;
        private readonly IDataProtectionProvider _provider;

        public Commerce(SiteContext context, IDataProtectionProvider provider = null)
        {
            _context = context;
            _provider = provider;
        }

        ApiKeys _apiKey;
        private string _priceSpace;
        private string _currencyPosition;
        private List<Dictionary<string, object>> _paymentInfoErrors = new List<Dictionary<string, object>>();

        public Order Order;

        public Dictionary<decimal, decimal> CalculatePercentageOfTaxWithPrice(Dictionary<decimal, decimal> prices, decimal total)
        {
            Dictionary<decimal, decimal> percentages = new Dictionary<decimal, decimal>();
            foreach (decimal key in prices.Keys)
            {
                percentages.Add(key, (100 / total) * prices[key]);
            }

            return percentages;
        }

        public string GetCurrency(string currency)
        {
            switch (currency.ToUpper())
            {
                case "EUR":
                    return "€";
                case "USD":
                case "AUD":
                case "BSD":
                case "XCD":
                case "ARS":
                case "BBD":
                case "BZD":
                case "BMD":
                case "BND":
                case "SGD":
                case "CAD":
                case "KYD":
                case "CLP":
                case "COP":
                case "NZD":
                case "CUC":
                case "CUP":
                    return "$";
                case "GBP":
                case "GGP":
                    return "£";
                default:
                    return "$";
            }
        }

        public void SetPriceFormatVariables(int websiteId)
        {
            SettingClient setting = new SettingClient(_context);
            _priceSpace = setting.GetSettingValueByKey("priceSpace", "website", websiteId).ToLower();
            _currencyPosition = setting.GetSettingValueByKey("currencyPosition", "website", websiteId).ToLower();
        }

        public string GetPriceFormat(string price, string currency)
        {
            currency = GetCurrency(currency);
            string space = _priceSpace == "true" ? " " : "";
            return _currencyPosition == "left" ? currency + space + price : price + space + currency;
        }

        public bool IsPromoEnabled(bool schedule, DateTime from, DateTime to, decimal promoPrice)
        {
            //If schedule is enabled for the promo price
            if (schedule)
            {
                DateTime dateTime = DateTime.Now;
                if (dateTime.Ticks > from.Ticks && dateTime.Ticks < to.Ticks)
                {
                    //Set promo to true if promo price is filled
                    return (promoPrice > 0) ? true : false;
                }
            }
            else
            {
                //Set promo to true if promo price is filled
                return (promoPrice > 0) ? true : false;
            }

            return false;
        }

        //public async Task CreatePdfAsync(OrderBundle orderBundle)
        //{
        //    UrlHelper urlHelper = new UrlHelper(_actionContextAccessor.ActionContext);
        //
        //    RouteValueDictionary routeValueDictionary = new RouteValueDictionary()
        //    {
        //        { "id", orderBundle.Order.Id }
        //    };
        //
        //    string baseUrl = _actionContextAccessor.ActionContext.HttpContext.Request.Scheme + "://" + _actionContextAccessor.ActionContext.HttpContext.Request.Host;
        //    string urlAction = urlHelper.Action("Index", "Pdf", routeValueDictionary);
        //    try
        //    {
        //        using (var client = new HttpClient())
        //        {
        //            client.BaseAddress = new Uri(baseUrl);
        //            var result = await client.GetAsync(urlAction);
        //            if (result.IsSuccessStatusCode && result.StatusCode == HttpStatusCode.OK)
        //            {
        //                await result.Content.ReadAsStringAsync();
        //            }
        //        }
        //
        //    }
        //    catch (Exception e)
        //    {
        //        Console.Write(e);
        //    }
        //}

        public async Task SendCloudLabelAsync(Orders order, int websiteId)
        {
            SendCloudClient sendCloud = new SendCloudClient(_apiKey.ClientId, _apiKey.ClientSecret);
            Dictionary<string, object> dic = new Dictionary<string, object>();
            //Dictionary<string, object> dic = new Dictionary<string, object>()
            //    {
            //        { "order_number", order.OrderNumber }
            //    };

            //dic = await sendCloud.GetAsync(dic, "parcel", "parcels");

            //foreach(KeyValuePair<string, object> parcels in dic)
            //{
            //    var regex = new Regex(Regex.Escape("{["));
            //    var obj = regex.Replace(parcels.Value.ToString(), "", 1);
            //    regex = new Regex(Regex.Escape("]}"));
            //    obj = regex.Replace(obj, "", obj.LastIndexOf("]}"));

            //    List<Dictionary<string, object>> list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(obj);
            //    foreach (Dictionary<string, object> parcel in list)
            //    {
            //        await sendCloud.CancelAsync(Int32.Parse(parcel.FirstOrDefault(x => x.Key == "id").Value.ToString()));
            //    }
            //}

            if (order.Status.ToLower() == "completed")
            {
                //Encryptor encryptor = new Encryptor(_provider);
                dic = new Dictionary<string, object>()
                {
                    { "name", (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1) ? order.ShippingFirstName + " " + order.ShippingLastName : order.BillingFirstName + " " + order.BillingLastName) },
                    { "address", (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1) ? order.ShippingAddressLine1 :  order.BillingAddressLine1) },
                    { "city", (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1) ? order.ShippingCity :  order.BillingCity) },
                    { "postal_code", (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1) ? order.ShippingZipCode  :  order.BillingZipCode) },
                    //{ "telephone", encryptor.Decrypt(_order.BillingPhoneNumber) },
                    { "email", order.BillingEmail },
                    { "country", (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1) ? order.ShippingCountry  :  order.BillingCountry).ToUpper() },
                    //{ "weight", totalWeight },
                    //{ "data", null},
                    { "order_number", order.OrderNumber }
                };

                //Request label if shipping method is found
                //if(shippingMethod != null)
                //{
                //    dic.Add("request_label", true);
                //    dic.Add("shipment", shippingMethod);
                //}

                if (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1))
                {
                    if (!string.IsNullOrWhiteSpace(order.ShippingCompany))
                    {
                        dic.Add("company_name", order.ShippingCompany);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(order.BillingCompany))
                    {
                        dic.Add("company_name", order.BillingCompany);
                    }
                }

                await sendCloud.CreateAsync(dic, "parcel", "parcels");
            }
        }

        public async Task RegisterOrderToAsync(Orders order, int websiteId)
        {
            //Create SendCloud label if there is an api key
            _apiKey = _context.ApiKeys.FirstOrDefault(ApiKey => ApiKey.Description.ToLower() == "sendcloud" && ApiKey.WebsiteId == websiteId);
            if (_apiKey != null)
            {
                await SendCloudLabelAsync(order, websiteId);
            }
            
            if (_apiKey != null)
            {
                //Update apiKey LastAccess
                _apiKey.LastAccess = DateTime.Now;
                new ApiKey(_context).UpdateApiKey(_apiKey);
            }
        }
    }
}