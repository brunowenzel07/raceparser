using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using XMLFeedParser.Config;
using XMLFeedParser.Model;

namespace XMLFeedParser.Parsers
{
    class HkjcParser : Parser
    {
        //TODO check when the data is available on wednesday and sundays
        protected override void parseRaces(IEnumerable<RaceStatus> racesToRetrieve, 
            out List<Meeting> _meetings, out List<Race> _races, out List<Runner> _runners, out List<RaceOdds> _odds)
        {
            //var meetings = new List<Meeting>();
            var races = new List<Race>();
            var runners = new List<Runner>();
            //var odds = new List<RaceOdds>();

            var date = racesToRetrieve.First().DateUTC.ToString("dd-MM-yyy");
            var venue = racesToRetrieve.First().MeetingCode;

            if (venue == null)
            {
                Log.Instance.Warn(Name + ": failed to parse, venue is NULL!");
            }
            else
            {
                List<Thread> threads = new List<Thread>();

                //to know if the race hasn't been refreshed for a while -> is done
                var localUpdateTime = new Dictionary<Race, DateTime>();

                foreach (var raceInfo in racesToRetrieve)
                {
                    //THREAD 3: WIN AND PLACE ODDS
                    var th3 = new Thread(
                        (object param) =>
                        {
                            var currentRace = (RaceStatus)param;
                            try
                            {
                                //REQUEST XML
                                XDocument doc = retrieveXML("jcbwracing_winplaodds", date, venue, currentRace.RaceNumber);

                                var thisRaceRunners = new List<RunnerHKG>();

                                //parse race elem
                                var content = doc.Root.Value;
                                var indexAt = content.IndexOf("@");
                                if (indexAt == 0)
                                {
                                    //data not available --> meeting not ready or already finished
                                    return;
                                }
                                //else
                                //{
                                //    var lastRefresh = content.Substring(0, indexAt));
                                //}
                                var indexWin = content.IndexOf("WIN") + 4;
                                var indexPla = content.IndexOf("#PLA");

                                var winChunks = content.Substring(indexWin, indexPla - indexWin).Split(';');
                                for (int i = 0; i < winChunks.Length; i++)
                                {
                                    RunnerHKG runner = new RunnerHKG();
                                    runner.MeetingId = currentRace.MeetingId;
                                    runner.RaceNumber = currentRace.RaceNumber;
                                    thisRaceRunners.Add(runner);

                                    var smallChunks = winChunks[i].Split('=');
                                    if (smallChunks.Length == 3)
                                    {
                                        runner.HorseNumber = int.Parse(smallChunks[0]); //runner no
                                        if (smallChunks[1] != "SCR")
                                        {
                                            runner.WinOdds = float.Parse(smallChunks[1], CultureInfo.InvariantCulture); //win odd
                                        }
                                        var inf = int.Parse(smallChunks[2]); //info (favourite, drop)
                                        runner.IsWinFavourite = inf == 1;
                                        runner.WinDropBy20 = inf == 2;
                                        runner.WinDropBy50 = inf == 3;
                                    }
                                    else
                                    {
                                        Log.Instance.Warn("Could not parse elem " + winChunks[i]);
                                    }
                                }

                                var plaChunks = content.Substring(indexPla + 5).Split(';');
                                for (int i = 0; i < plaChunks.Length; i++)
                                {
                                    RunnerHKG runner = thisRaceRunners[i];

                                    var smallChunks = plaChunks[i].Split('=');
                                    if (smallChunks.Length == 3)
                                    {
                                        var rno = int.Parse(smallChunks[0]); //runner no
                                        if (rno == runner.HorseNumber)
                                        {
                                            if (smallChunks[1] != "SCR")
                                            {
                                                runner.Placeodds = float.Parse(smallChunks[1], CultureInfo.InvariantCulture); //place odd
                                            }
                                            var inf = int.Parse(smallChunks[2]); //info (favourite, drop)
                                            runner.IsPlaceFavourite = inf == 1;
                                            runner.PlaceDropBy20 = inf == 2;
                                            runner.PlaceDropBy50 = inf == 3;
                                        }
                                        else
                                            Log.Instance.Warn("Could not parse elem " + plaChunks[i]);
                                    }
                                    else
                                    {
                                        Log.Instance.Warn("Could not parse elem " + plaChunks[i]);
                                    }
                                }

                                lock (runners)
                                {
                                    runners.AddRange(thisRaceRunners);
                                }

                                log("jcbwracing_winplaodds", date, venue, currentRace.RaceNumber, -1, null);
                            }
                            catch (Exception e)
                            {
                                log("jcbwracing_winplaodds", date, venue, currentRace.RaceNumber, -1, e);
                            }
                        }); //thread

                    threads.Add(th3);
                    th3.Start(raceInfo);
                    //th3.Join();  //test


                    //THREAD 4: POOL TOTAL
                    var th4 = new Thread(
                        (object param) =>
                        {
                            var currentRace = (RaceStatus)param;
                            try
                            {
                                XDocument doc = retrieveXML("pooltot", date, venue, currentRace.RaceNumber);

                                RaceHKG race = new RaceHKG();
                                race.MeetingId = currentRace.MeetingId;
                                race.RaceNumber = currentRace.RaceNumber;
                                race.RaceJumpTimeUTC = currentRace.JumpTimeUTC;

                                if (doc.Root.Attribute("ERR") == null)
                                {
                                    string dt = doc.Root.Attribute("updateDate").Value + " " +
                                                doc.Root.Attribute("updateTime").Value;
                                    lock (localUpdateTime)
                                    {
                                        localUpdateTime.Add(race, DateTime.ParseExact(dt, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                                    }

                                    //parse race elem
                                    var baseElem = doc.Root.Element("RACE");
                                    baseElem.Elements("INV").ToList().ForEach(
                                        elem =>
                                        {
                                            var poolType = (string)elem.Attribute("POOL");
                                            switch (poolType)
                                            {
                                                case "WIN":
                                                    race.RaceWinPool = float.Parse(elem.Value, CultureInfo.InvariantCulture);
                                                    break;
                                                case "PLA":
                                                    race.RacePPPool = float.Parse(elem.Value, CultureInfo.InvariantCulture);
                                                    break;
                                                default:
                                                    //Log.Instance.Warn("Pool type not registered: " + poolType);
                                                    break;
                                            }
                                        });

                                    log("pooltot", date, venue, -1, currentRace.RaceNumber, null);
                                }

                                lock (races)
                                {
                                    races.Add(race);
                                }

                            }
                            catch (Exception e)
                            {
                                log("pooltot", date, venue, -1, currentRace.RaceNumber, e);
                            }
                        }); //thread

                    threads.Add(th4);
                    th4.Start(raceInfo);
                    //th4.Join(); //test

                } //foreach


                var nextRace = -1;
                var scratched = new Dictionary<int, List<int>>();

                //THREAD 1: SCRATCHED
                var th1 = new Thread(() =>
                {
                    try
                    {
                        //REQUEST XML
                        XDocument doc = retrieveXML("jcbwracing_scratched", date, venue, -1);

                        //parse race elem
                        if ((string)doc.Root.Attribute("RAN_RACE") == "")
                        {
                            //data not available --> meeting not ready or already finished
                            return;
                        }
                        else
                        {
                            nextRace = (int)doc.Root.Attribute("RAN_RACE");
                        }

                        var chunks = doc.Root.Value.Split(';');
                        for (int i = 1; i < chunks.Length; i++)
                        {
                            var smallChunks = chunks[i].Split('#');
                            if (smallChunks.Length == 2)
                            {
                                var race = int.Parse(smallChunks[0]); //race no
                                var runner = int.Parse(smallChunks[1]); //runner no
                                
                                List<int> scRunners;
                                if (!scratched.TryGetValue(race, out scRunners))
                                {
                                    scRunners = new List<int>();
                                    scratched.Add(race, scRunners);
                                }
                                scRunners.Add(runner);
                            }
                            else
                            {
                                Log.Instance.Warn("Could not parse elem " + doc.Root.Value);
                            }
                        }
                        log("jcbwracing_scratched", date, venue, -1, -1, null);
                    }
                    catch (Exception e)
                    {
                        log("jcbwracing_scratched", date, venue, -1, -1, e);
                    }
                }); //thread

                threads.Add(th1);
                th1.Start();
                //th1.Join(); 


                //wait til all threads finish
                threads.ForEach(th => th.Join());


                foreach (var pair in scratched)
                {
                    foreach (var scRunner in pair.Value)
                    {
                        if (runners.Exists(r => r.RaceNumber == pair.Key && r.HorseNumber == scRunner))
                            runners.Find(r => r.RaceNumber == pair.Key && r.HorseNumber == scRunner).isScratched = true;
                    }
                }

                races.FindAll(r => r.RaceNumber < nextRace).ForEach(r => r.RaceStatus = ConfigValues.RaceStatusDone);
                races.FindAll(r => r.RaceNumber >= nextRace).ForEach(r =>
                    {
                        r.RaceStatus = ConfigValues.RaceStatusAlive;
                        if (r.RaceNumber == nextRace)
                        {
                            //check if it hasn't been refreshed for a while. After 5 min inactive, it's considered done
                            var stateId = racesToRetrieve.First(rr => rr.MeetingId == r.MeetingId).AUS_StateId;
                            var refTimeUTC = TimeZoneHelper.ToUTC(localUpdateTime[r], stateId);
                            var diff = DateTime.UtcNow - refTimeUTC;
                            if (diff.TotalSeconds > ConfigValues.SecsInactiveForRaceDone)
                                r.RaceStatus = ConfigValues.RaceStatusDone;
                            //TODO check if this works with the last race of the day
                        }
                    });
                //TODO how do we know that a race has been abandoned?
            }

            _meetings = null;
            _races = races;
            _runners = runners;
            _odds = null;
        }

        private XDocument retrieveXML(string type, string date, string venue, int raceno)
        {
            string xml;
            using (var webClient = new GZipWebClient())
            {
                webClient.QueryString.Add("type", type);
                webClient.QueryString.Add("date", date);
                webClient.QueryString.Add("venue", venue);

                if (type == "pooltot")
                {
                    webClient.QueryString.Add("raceno", raceno.ToString());
                }

                if (type == "jcbwracing_winplaodds")
                {
                    webClient.QueryString.Add("start", raceno.ToString());
                    webClient.QueryString.Add("end", raceno.ToString());
                }

                //webClient.Encoding = Encoding.UTF8;

                xml = webClient.DownloadString(BaseUrl);
                return XDocument.Parse(xml);
            }
        }

        public class GZipWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                return request;
            }
        }

        private void log(string type, string date, string venue, int start, int raceno, Exception e)
        {
            string p = string.Format("{0}?type={1}&date={2}&venue={3}{4}{5}",
                BaseUrl, type, date, venue,
                start > -1 ? "&start=" + start + "&end=" + start : "",
                raceno > -1 ? "&raceno=" + raceno : "");

            if (e == null)
                Log.Instance.Debug(Name + ": successfully parsed " + p);
            else
                Log.Instance.WarnException(Name + ": failed to parse " + p, e);
        }


        protected override void updateRaces(List<Meeting> meetings, List<Race> races, List<Runner> runners, List<RaceOdds> odds)
        {
            if (runners.Count() > 0)
                DBGatewayHkjc.UpdateHKGData(races.Cast<RaceHKG>(), runners.Cast<RunnerHKG>());
        }

    }
}
