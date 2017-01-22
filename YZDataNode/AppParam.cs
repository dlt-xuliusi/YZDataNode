using System;
using System.Collections.Generic;
using System.Configuration;

namespace YZDataNode
{
    public class AppParam
    {
        public static bool _autoStart = false;

        public static string _appName = string.Empty;
        public static string _appId = string.Empty;

        public static string _dataCenterIp = string.Empty;
        public static ushort _dataCenterPort = 0;

        public static string MsisdnSubscript = string.Empty;
        public static string CgiSubscript = string.Empty;
        public static string AreaCodeSubscript = string.Empty;
        public static string StatFileDir = string.Empty;

        public static void Init()
        {
            _appName = GetConfigVauleRaw("AppName", string.Empty);
            _appId = GetConfigVauleRaw("AppId", string.Empty);

            _dataCenterIp = GetConfigVauleRaw("DateCenterIP", "127.0.0.1");
            _dataCenterPort = (ushort)AppHelper.IntParse(GetConfigVauleRaw("DateCenterPort", "8808"));

            MsisdnSubscript = GetConfigVauleRaw("MsisdnSubscript", string.Empty);
            CgiSubscript = GetConfigVauleRaw("CgiSubscript", string.Empty);
            AreaCodeSubscript = GetConfigVauleRaw("AreaCodeSubscript", string.Empty);
            StatFileDir = GetConfigVauleRaw("StatFileDir", string.Empty);
        }

        public static void GetValueItem(string itemName, List<string> listGroup)
        {
            for (int i = 0; i < 1000; i++)
            {
                string item = string.Format("{0}{1}", itemName, i + 1);
                string value = GetConfigVauleRaw(item, string.Empty);
                if (string.IsNullOrEmpty(value))
                {
                    break;
                }
                listGroup.Add(value);
            }
        }

        public static string GetConfigVauleRaw(string name, string defaultvalue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[name];
                return value;
            }
            catch (Exception ex)
            {
                return defaultvalue;
            }
        }

        public static int GetConfigVauleInt(string name, int defaultvalue = 0)
        {
            try
            {
                string str = GetConfigVauleRaw(name, defaultvalue.ToString());
                return int.Parse(str);
            }
            catch (Exception ex)
            {
                return defaultvalue;
            }
        }

        public static bool SaveConfigValueRaw(string newKey, string newValue)
        {
            try
            {
                bool isModified = false;
                foreach (string key in ConfigurationManager.AppSettings)
                {
                    if (key == newKey)
                    {
                        isModified = true;
                    }
                }

                // Open App.Config of executable
                Configuration config =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                // You need to remove the old settings object before you can replace it
                if (isModified)
                {
                    config.AppSettings.Settings.Remove(newKey);
                }
                // Add an Application Setting.
                config.AppSettings.Settings.Add(newKey, newValue);
                // Save the changes in App.config file.
                config.Save(ConfigurationSaveMode.Modified);
                // Force a reload of a changed section.
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //public static string GetAppVer()
        //{
        //    try
        //    {
        //        string ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //        DateTime date = System.IO.File.GetLastWriteTime(typeof(AppParam).Assembly.Location);
        //        return string.Format("--版本:{0}--日期:{1}", ver, AppHelper.DateTimeToStr(date));
        //    }
        //    catch (Exception ex)
        //    {
        //        return string.Empty;
        //    }
        //}
    }
}