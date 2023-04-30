using Microsoft.AspNetCore.Mvc;
using Site.Data;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using Microsoft.ApplicationInsights.AspNetCore;
using System.Resources;
using System.Reflection;

namespace Site.Models.Setting
{
    public partial class SettingClient : Controller
    {
        private readonly SiteContext _context;
        private readonly ResourceManager _resourceManager;

        public SettingClient(SiteContext context)
        {
            _context = context;

            _resourceManager = new ResourceManager("Site.Resources.Models.Setting.SettingClient", Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// First validate before updating the table row
        /// </summary>
        /// <param name="setting">Settings table row</param>
        public ObjectResult ValidateAndUpdateSetting(Settings setting, int websiteId)
        {
            if (setting.Value == null) setting.Value = "";

            if (ValidateSettingType(setting.Value) != "")
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _resourceManager.GetString("SettingNotAvailable")
                }));
       
            Settings _setting = GetSettingById(setting.Id);
            if (_setting.LinkedToType.ToLower() == "website")
                if (_setting.LinkedToId != websiteId)
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _resourceManager.GetString("NoAccessToSetting")
                    }));

            // Validating value based on key
            _setting.Value = setting.Value;
            string result = ValidateSettingByKey(_setting);
            if (result != "")
                return StatusCode(400, result);

            // Update setting
            UpdateSetting(_setting);

            return Ok(Json(new
            {
                messageType = "success",
                message = _resourceManager.GetString("SettingUpdated")
            }));
        }

        public ObjectResult GetCommerceSettings(int websiteId)
        {
            return Ok(new Dictionary<string, object>()
            {
                { "invoicePrefix", GetSettingByKey("invoicePrefix", "website", websiteId) },
                { "orderPrefix", GetSettingByKey("orderPrefix", "website", websiteId) },
                { "creditPrefix", GetSettingByKey("creditPrefix", "website", websiteId) },
                { "invoiceSuffix", GetSettingByKey("invoiceSuffix", "website", websiteId) },
                { "orderSuffix", GetSettingByKey("orderSuffix", "website", websiteId) },
                { "creditSuffix", GetSettingByKey("creditSuffix", "website", websiteId) },
                { "invoiceCurrent", GetSettingByKey("invoiceCurrent", "website", websiteId) },
                { "orderCurrent", GetSettingByKey("orderCurrent", "website", websiteId) },
                { "addressLine1", GetSettingByKey("addressLine1", "website", websiteId) },
                { "zipCode", GetSettingByKey("zipCode", "website", websiteId) },
                { "city", GetSettingByKey("city", "website", websiteId) },
                { "coc", GetSettingByKey("coc", "website", websiteId) },
                { "vat", GetSettingByKey("vat", "website", websiteId) },
                { "country", GetSettingByKey("country", "website", websiteId) }
            });
        }
    }
}