using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MyBlog.Model
{
    public static class CommonLogic
    {
        public static string ToUrlSlug(this string value)
        {
            //First to lower case 
            value = value.ToLowerInvariant();

            //Remove all accents
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(value);

            value = Encoding.ASCII.GetString(bytes);

            //Convert multiple spaces into one space   
            value = Regex.Replace(value, @"\s+", " ").Trim();

            //Remove all non valid chars
            value = Regex.Replace(value, @"[^a-z0-9\s-]", ""); 

            //Replace spaces 
            value = Regex.Replace(value, @"\s", "-", RegexOptions.Compiled);

            //Remove invalid chars 
            value = Regex.Replace(value, @"[^\w\s\p{Pd}]", "", RegexOptions.Compiled);

            //Trim dashes from end 
            value = value.Trim('-', '_');

            //Replace double occurences of - or \_ 
            value = Regex.Replace(value, @"([-_]){2,}", "$1", RegexOptions.Compiled);

            return value;
        }

        public static SettingViewModel GetSetting()
        {
            SettingViewModel setting = CacheHelper.Get<SettingViewModel>(CacheKey.Setting);
            try
            {
                if (setting == null)
                {
                    string filePath = IO.PathCombine(System.Web.HttpContext.Current.Server.MapPath("~"), "setting.json");
                    string fileContent = IO.GetFileContents(filePath);
                    setting = JsonConvert.DeserializeObject<SettingViewModel>(fileContent);
                    CacheHelper.ClearOrAdd(CacheKey.Setting, setting, CacheKey.SettingCacheTimeSeconds);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                setting = new SettingViewModel();
            }

            return setting;
        }

        public static void SetSetting(SettingViewModel setting)
        {
            CacheHelper.ClearOrAdd(CacheKey.Setting, setting, CacheKey.SettingCacheTimeSeconds);
            string filePath = IO.PathCombine(System.Web.HttpContext.Current.Server.MapPath("~"), "setting.json");
            string json = JsonConvert.SerializeObject(setting);
            IO.WriteFileContents(filePath, json);
        }
    }
}
