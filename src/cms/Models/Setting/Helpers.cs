using Microsoft.ApplicationInsights.AspNetCore;
using Site.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Site.Models.Setting
{
    public partial class SettingClient
    {
        /// <summary>
        /// Returns all settings that are related to E-Commerce.
        /// </summary>
        /// <param name="productTemplate">ProductTemplates table row</param>
        /// <param name="websiteId">Integer of the website</param>
        public Dictionary<string, object> ConvertEcommerceSettingsToJson(ProductTemplates productTemplate, int websiteId)
        {
            return new Dictionary<string, object>()
            {
                { "crossSells", productTemplate.CrossSells },
                { "downloadable", productTemplate.Downloadable },
                { "externalProduct", productTemplate.ExternalProduct },
                { "groupedProduct", productTemplate.GroupedProduct },
                { "reviews", productTemplate.Reviews },
                { "simpleProduct", productTemplate.SimpleProduct },
                { "upsells", productTemplate.Upsells },
                { "variableProduct", productTemplate.VariableProduct },
                { "virtual", productTemplate.Virtual },
                { "digitsAfterDecimal", GetSettingValueByKey("digitsafterdecimal", "website", websiteId) }
            };
        }

        /// <summary>
        /// Validates if the user has access to change the setting. 
        /// Only settings with a type that contains in the list are editable.
        /// </summary>
        /// <param name="type"> Setting type</param>
        public string ValidateSettingType(string type)
        {
            List<string> validTargets = new List<string>() {
                "invoicePrefix", "creditPrefix", "orderPrefix",
                "invoiceSuffix", "creditSuffix", "orderSuffix",
                "invoiceCurrent", "orderCurrent", "addressLine1",
                "zipCode", "city", "coc",
                "vat", "country"
            };

            return (validTargets.Any(type.Contains)) ? type : "";
        }

        /// <summary>
        /// Validates Setting value based on key
        /// </summary>
        /// <param name="setting">Settings table row</param>
        public string ValidateSettingByKey(Settings setting)
        {
            switch(setting.Key.ToLower())
            {
                case "invoiceprefix":
                case "creditprefix":
                case "orderprefix":
                case "invoicesuffix":
                case "creditsuffix":
                case "ordersuffix":
                    if (setting.Value.Length > 4)
                    {
                        return _resourceManager.GetString("CannotLongerThan4Characters");
                    }
                    break;
                case "invoicecurrent":
                case "ordercurrent":
                    if (setting.Value.Length > 12)
                    {
                        return _resourceManager.GetString("CannotLongerThan12Numbers");
                    }

                    if (setting.Value.Length < 4)
                    {
                        return _resourceManager.GetString("CannotLessThan4Numbers");
                    }

                    if (!Regex.Match(setting.Value, @"[0-9]*", RegexOptions.IgnoreCase).Success)
                    {
                        return _resourceManager.GetString("CanOnlyFillNumbers");
                    }
                    break;
                case "addressline1":
                case "city":
                case "country":
                case "coc":
                case "vat":
                    if (setting.Value.Length > 250)
                    {
                        return _resourceManager.GetString("CannotLongerThan250Characters");
                    }
                    break;
                case "zipcode":
                    if (setting.Value.Length > 32)
                    {
                        return _resourceManager.GetString("CannotLongerThan32Characters");
                    }
                    break;
            }

            return "";
        }
    }
}
