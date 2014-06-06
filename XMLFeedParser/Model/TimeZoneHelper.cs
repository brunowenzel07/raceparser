using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using XMLFeedParser.Config;

namespace XMLFeedParser.Model
{
    static class TimeZoneHelper
    {
        private static Dictionary<int, string> winRegEntries;

        static TimeZoneHelper()
        {
            var config = loadConfigFile();
            var codes = DBGateway.GetTimeZones();

            winRegEntries = new Dictionary<int, string>();
            codes.ToList().ForEach(c =>
            {
                string timeZoneCode;
                if (!config.TryGetValue(c.Value, out timeZoneCode))
                    throw new Exception("TimeZone code " + c.Value + " not found on TimeZones.config");
                winRegEntries.Add(c.Key, timeZoneCode);
            });
        }

        private static Dictionary<string, string> loadConfigFile()
        {
            Dictionary<string, string> config = new Dictionary<string,string>();

            XDocument doc = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "timezones.config");
            doc.Root.Elements("timeZone").ToList().ForEach(t =>
                config.Add((string)t.Attribute("tz"), (string)t.Attribute("winRegEntry")));
            
            return config;
        }

        public static DateTime ToUTC(DateTime dt, int AUS_StateId)
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(winRegEntries[AUS_StateId]);
            return TimeZoneInfo.ConvertTimeToUtc(dt, tz);
        }

        public static TimeSpan MelbourneToUTC(TimeSpan ts)
        {
            var utcNow = DateTime.UtcNow;
            DateTime dt = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 
                                       ts.Hours, ts.Minutes, ts.Seconds);
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(ConfigValues.MelbourneStandardTime);
            return TimeZoneInfo.ConvertTimeToUtc(dt, tz).TimeOfDay;
        }

    }
}
