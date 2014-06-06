using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using XMLFeedParser.Config;
using XMLFeedParser.Model;

namespace XMLFeedParser.Parsers
{
    class TattsRaceDayParser: IParser
    {
        public string BaseUrl { get; set; }

        public string Name { get; set; }

        public string CountryCode { get; set; }

        IEnumerable<TimeSpan> updateTimes;
        DateTime nextUpdate = DateTime.MinValue;


        public TattsRaceDayParser()
        {
            var strTimes = ConfigValues.RaceDayUpdateTimes.Split(',');
            var tsTimes = strTimes.Select(t => TimeSpan.Parse(t));
            updateTimes = tsTimes.Select(t => TimeZoneHelper.MelbourneToUTC(t)).OrderBy(t => t);
        }

        int numErrors = 0;
        public void Dojob()
        {
            DateTime utcNow = DateTime.UtcNow;

            if (utcNow > nextUpdate)
            {
                if (updateRaceDay())
                {
                    //set next update time
                    var nextTS = updateTimes.FirstOrDefault(dt => dt > utcNow.TimeOfDay);
                    if (nextTS == default(TimeSpan))
                    {
                        nextTS = updateTimes.First();
                        nextUpdate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day + 1, //tomorrow
                                                  nextTS.Hours, nextTS.Minutes, nextTS.Seconds);
                    }
                    else //today
                    {
                        nextUpdate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day,
                                                  nextTS.Hours, nextTS.Minutes, nextTS.Seconds);
                    }

                    numErrors = 0;
                }
                else //there was an error, increase delay with the number of errors
                {
                    numErrors++;
                    nextUpdate = utcNow.AddSeconds(numErrors * numErrors * ConfigValues.DelayIfErrorSec);
                }
            }
        }


        bool updateRaceDay()
        {
            try
            {
                var meetings = parseRaceDay();
                DBGatewayTatts.UpdateTattsData(meetings, null, null, null);
                return true;
            }
            catch (Exception e)
            {
                Log.Instance.ErrorException(Name + ": failed to update database with RaceDay.xml", e);
                return false;
            }
        }


        List<Meeting> parseRaceDay()
        {
            var meetings = new List<Meeting>();

            //parse today and tomorrow meetings
            var dts = new List<DateTime>();
            dts.Add(DateTime.UtcNow.Date);
            dts.Add(DateTime.UtcNow.Date.AddDays(1));

            foreach (var date in dts)
            {
                string url = "";
                try
                {
                    //REQUEST XML
                    url = string.Format("{0}/{1}/raceday.xml",
                        BaseUrl,
                        date.ToString("yyyy/M/d"));

                    string xml;
                    using (var webClient = new WebClient())
                    {
                        xml = webClient.DownloadString(url);
                    }

                    XDocument doc = XDocument.Parse(xml);


                    //parse meeting elem
                    foreach (var meetElem in doc.Root.Elements("Meeting"))
                    {
                        meetings.Add(new Meeting
                        {
                            isAbandoned = (string)meetElem.Attribute("MtgAbandoned") == ConfigValues.YES,
                            RacecourseName = (string)meetElem.Attribute("VenueName"),
                            MeetingDate = date,
                            NumberOfRaces = meetElem.Elements("Race").Count(),
                            MeetingCode = (string)meetElem.Attribute("MeetingCode")
                        });
                    }

                    Log.Instance.Debug(Name + ": successfully parsed " + url);
                }
                catch (Exception e)
                {
                    Log.Instance.WarnException(Name + ": failed to parse " + url, e);
                }
            }

            return meetings;
        }
    }
}
