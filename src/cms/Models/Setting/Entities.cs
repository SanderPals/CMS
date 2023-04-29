using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Site.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Site.Models.Setting
{
    public partial class SettingClient
    {
        /// <summary>
        /// Returns setting based on params. 
        /// </summary>
        /// <param name="Id">Integer column of Settings table</param>
        public Settings GetSettingById(int id)
        {
            return _context.Settings.FirstOrDefault(Setting => Setting.Id == id);
        }

        /// <summary>
        /// Returns setting based on params. 
        /// </summary>
        /// <param name="key">Key column of Settings table</param>
        /// <param name="linkedToType">LinkedToType column of Settings table</param>
        /// <param name="linkedToId">LinkedToId column of Settings table</param>
        public Settings GetSettingByKey(string key, string linkedToType, int linkedToId)
        {
            return _context.Settings.FirstOrDefault(Setting => Setting.Key == key && Setting.LinkedToType == linkedToType && Setting.LinkedToId == linkedToId);
        }

        /// <summary>
        /// Returns setting value based on params. 
        /// If setting is not found it will returns an empty string
        /// </summary>
        /// <param name="key">Key column of Settings table</param>
        /// <param name="linkedToType">LinkedToType column of Settings table</param>
        /// <param name="linkedToId">LinkedToId column of Settings table</param>
        public string GetSettingValueByKey(string key, string linkedToType, int linkedToId)
        {
            Settings _setting = _context.Settings.FirstOrDefault(Setting => Setting.Key == key && Setting.LinkedToType == linkedToType && Setting.LinkedToId == linkedToId);

            return _setting != null ? _setting.Value : "";
        }

        /// <summary>
        /// Increase setting value and returns old one
        /// </summary>
        /// <param name="key">Key column of Settings table</param>
        /// <param name="linkedToType">LinkedToType column of Settings table</param>
        /// <param name="linkedToId">LinkedToId column of Settings table</param>
        public async Task<Settings> IncrementSettingValueByKeyAsync(string key, string linkedToType, int linkedToId)
        {
            return await _context.Settings.FromSql("Increase_Setting_Value @Key, @LinkedToType, @LinkedToId", new SqlParameter("Key", key), 
                                                                                                              new SqlParameter("LinkedToType", linkedToType), 
                                                                                                              new SqlParameter("LinkedToId", linkedToId)).FirstOrDefaultAsync();//.FromSql("Increase_Setting_Value @Key, @LinkedToType, @LinkedToId", key, linkedToType, linkedToId.ToString()).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Update Settings table row
        /// </summary>
        /// <param name="setting">Settings table row</param>
        public void UpdateSetting(Settings setting)
        {
            _context.Settings.Update(setting);
            _context.SaveChanges();
        }
    }
}
