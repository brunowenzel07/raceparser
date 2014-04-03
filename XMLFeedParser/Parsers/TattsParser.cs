using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XMLFeedParser.Model;
using System.Xml.XPath;
using System.Net;
using System.Xml.Linq;
using System.Data;
using System.Threading;
using XMLFeedParser.Config;


namespace XMLFeedParser.Parsers
{
    class TattsParser : Parser
    {
        protected override void parseRaces(IEnumerable<RaceStatus> racesToRetrieve, 
            out List<Meeting> _meetings, out List<Race> _races, 
            out List<Runner> _runners, out List<RaceOdds> _odds)
        {
            var meetings = new List<Meeting>();
            var races = new List<Race>();
            var runners = new List<Runner>();
            var odds = new List<RaceOdds>();

            var processedmeetings = new Dictionary<int, bool>();

            List<Thread> threads = new List<Thread>();
            foreach (var raceInfo in racesToRetrieve)
	        {
                var th = new Thread(
                    (object param) =>
                    {
                        string url = "";
                        try
                        {
                            //TEST
                            var currentRace = (RaceStatus)param;
                            //var currentRace = raceInfo;

                            //REQUEST XML
                            url = string.Format("{0}/{1}/{2}{3}.xml", 
                                BaseUrl, 
                                currentRace.DateUTC.ToString("yyyy/M/d"),
                                currentRace.MeetingCode,
                                currentRace.RaceNumber);

                            string xml;
                            using (var webClient = new WebClient())
                            {
                                xml = webClient.DownloadString(url);
                            }

                            XDocument doc = XDocument.Parse(xml);

                            //parse race elem
                            string currentRaceStatus;
                            var meetElem = doc.Root.Element("Meeting");
                            var raceElem = meetElem.Element("Race");
                            var runnerElems = raceElem.Elements("Runner");
                            {

                                var dtLocal = (DateTime)raceElem.Attribute("RaceTime");
                                var aux = new RaceTatts
                                {
                                    MeetingId = currentRace.MeetingId,
                                    RaceNumber = currentRace.RaceNumber,
                                    RaceJumpTimeUTC = TimeZoneHelper.ToUTC(dtLocal, currentRace.AUS_StateId),
                                    RaceName = (string)raceElem.Attribute("RaceName"),
                                    DistanceName = (string)raceElem.Attribute("Distance"),
                                    TrackRating = (int)raceElem.Attribute("TrackRating"),
                                    isTrackChanged = (string)raceElem.Attribute("TrackChanged") == ConfigValues.YES,
                                    RaceStatus = currentRaceStatus = (string)raceElem.Attribute("RaceDisplayStatus"),
                                    NumberOfRunners = runnerElems.Count(),
                                    LocalJumpTime = dtLocal
                                };
                                lock (races)
                                { 
                                    races.Add(aux);
                                }
                            }

                            //parse meeting elem only for the first race (otherwise we would get duplicate meetings!)
                            lock (processedmeetings)
                            {
                                if (!processedmeetings.ContainsKey(currentRace.MeetingId))
                                {
                                    processedmeetings.Add(currentRace.MeetingId, false);

                                    var aux = new Meeting
                                    {
                                        MeetingId = currentRace.MeetingId,
                                        isAbandoned = (string)meetElem.Attribute("MtgAbandoned") == ConfigValues.YES,
                                        RacecourseName = (string)meetElem.Attribute("VenueName"),
                                        MeetingDate = currentRace.DateUTC,
                                        MeetingCode = (string)meetElem.Attribute("MeetingCode")
                                    };
                                    //lock (meetings)
                                    {
                                        meetings.Add(aux);
                                    }
                                }
                            }

                            var favElem = raceElem.Element("SubFavCandidate"); 
                            var fav = -1;
                            if (favElem != null)
                                fav = int.Parse((string)favElem.Attribute("RunnerNo"));

                            //parse each runner elem
                            {

                                //save runners' places to be set later
                                var places = new Dictionary<int, int>();
                                raceElem.Elements("ResultPlace").ToList().ForEach(result =>
                                {
                                    int place = (int)result.Attribute("PlaceNo");
                                    int runnerNo = (int)result.Element("Result").Attribute("RunnerNo");
                                    places.Add(runnerNo, place);
                                });

                                var aux = runnerElems.Select(
                                    runnerElem =>
                                    {
                                        int horseNo = (int)runnerElem.Attribute("RunnerNo");
                                        var place = 0;
                                        places.TryGetValue(horseNo, out place);
                                        var winOddsElem = runnerElem.Element("WinOdds"); //may be null when the meeting has been adandoned
                                        var placeOddsElem = runnerElem.Element("PlaceOdds"); //may be null when the meeting has been adandoned

                                        return new RunnerTatts
                                        {
                                            MeetingId = currentRace.MeetingId,
                                            RaceNumber = currentRace.RaceNumber,
                                            HorseNumber = horseNo,
                                            HorseName = (string)runnerElem.Attribute("RunnerName"),
                                            JockeyName = (string)runnerElem.Attribute("Rider"),
                                            Barrier = (string)runnerElem.Attribute("Barrier"),
                                            AUS_HcpWt = (string)runnerElem.Attribute("Weight"),
                                            isScratched = (string)runnerElem.Attribute("Scratched") == ConfigValues.YES,
                                            isJockeyChanged = (string)runnerElem.Attribute("RiderChanged") == ConfigValues.YES,
                                            Place = place,
                                            WinOdds = winOddsElem != null ? (float)winOddsElem.Attribute("Odds") : 0,
                                            Placeodds = placeOddsElem != null ? (float)placeOddsElem.Attribute("Odds") : 0,
                                            IsFavourite = horseNo == fav
                                        };
                                    });

                                lock (runners)
                                {
                                    runners.AddRange(aux);
                                }
                            }


                            //Odds are only to be parsed when the race is done
                            //if (currentRaceStatus == ConfigValues.RaceStatusDone)
                            {
                                var aux = new RaceOdds();
                                aux.MeetingId = currentRace.MeetingId;
                                aux.RaceNumber = currentRace.RaceNumber;
                                aux.RaceStatus = currentRaceStatus;

                                raceElem.Elements("Pool").ToList().ForEach(oddsElem =>
                                    {
                                        //var poolStatus = (string)oddsElem.Attribute("PoolDisplayStatus");

                                        float poolTotal = 0;
                                        if (oddsElem.Attribute("PoolTotal") != null)
                                            poolTotal = (float)oddsElem.Attribute("PoolTotal");
                                        
                                        float divAmount = 0;
                                        if (oddsElem.Element("Dividend") != null)
                                            divAmount = (float)oddsElem.Element("Dividend").Attribute("DivAmount");
                                        
                                        switch ((string)oddsElem.Attribute("PoolType"))
                                        {
                                            case "EX":
                                                aux.EXPoolTotal = poolTotal;
                                                aux.EXDivAmount = divAmount;
                                                break;
                                            case "F4":
                                                aux.F4PoolTotal = poolTotal;
                                                aux.F4DivAmount = divAmount;
                                                break;
                                            case "QN":
                                                aux.QNPoolTotal = poolTotal;
                                                aux.QNDivAmount = divAmount;
                                                break;
                                            case "TF":
                                                aux.TFPoolTotal = poolTotal;
                                                aux.TFDivAmount = divAmount;
                                                break;
                                            case "WW":
                                                aux.WWPoolTotal = poolTotal;
                                                aux.WWDivAmount = divAmount;
                                                break;
                                            case "PP":
                                                aux.PPPoolTotal = poolTotal;
                                                aux.PPDivAmount = divAmount;
                                                break;
                                            default:
                                                break;
                                        }
                                    });

                                lock (odds)
                                {
                                    odds.Add(aux);
                                }
                            }
                            Log.Instance.Debug(Name + ": successfully parsed " + url);
                        }
                        catch (Exception e)
                        {
                            Log.Instance.WarnException(Name + ": failed to parse " + url, e);
                        }
                    }); //thread

                    threads.Add(th);
                    th.Start(raceInfo);
                    //th.Join(); 

            } //foreach

            //wait til all threads finish
            threads.ForEach(th => th.Join());

            _meetings = meetings;
            _races = races;
            _runners = runners;
            _odds = odds;
        }


        protected override void updateRaces(List<Meeting> meetings, List<Race> races, List<Runner> runners, List<RaceOdds> odds)
        {
            DBGatewayTatts.UpdateTattsData(meetings, races.Cast<RaceTatts>(), runners.Cast<RunnerTatts>(), odds);
        }
    }



}
