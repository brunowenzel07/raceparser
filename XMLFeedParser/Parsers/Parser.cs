using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using XMLFeedParser.Config;
using XMLFeedParser.Model;

namespace XMLFeedParser.Parsers
{
    abstract class Parser: IParser
    {
        public string BaseUrl { get; set; }

        public string Name { get; set; }

        public string CountryCode { get; set; }

        private List<RaceStatus> racesStatus = new List<RaceStatus>();

        private DateTime nextUpcomingMeetingsRefresh = DateTime.UtcNow;



        protected abstract void parseRaces(IEnumerable<RaceStatus> racesToRetrieve,
                                    out List<Meeting> _meetings, out List<Race> _races, 
                                    out List<Runner> _runners,   out List<RaceOdds> _odds);

        protected abstract void updateRaces(List<Meeting> meetings, List<Race> races, 
                                            List<Runner> runners, List<RaceOdds> odds);


        public void Dojob()
        {
            DateTime utcNow = DateTime.UtcNow;

            if (utcNow > nextUpcomingMeetingsRefresh)
            {
                //upcoming meetings are loaded from DB
                if (loadRaceCodes())
                    nextUpcomingMeetingsRefresh = nextUpcomingMeetingsRefresh.AddSeconds(ConfigValues.UpcomingMeetingsRefreshInterval);
                else //there was an error -> retry after 10 seconds
                    nextUpcomingMeetingsRefresh = nextUpcomingMeetingsRefresh.AddSeconds(ConfigValues.DelayIfErrorSec);
            }


            var racesToProcess = racesStatus.Where(r => !r.IsDone && r.NextRefreshUTC <= utcNow);

            //List<Thread> activeThreads = new List<Thread>();
            //refresh active races: those not finished whose jumptime-25 has passed
            var activeRaces = racesToProcess.Where(r => r.ActiveTimeUTC <= utcNow).ToList();
            if (activeRaces.Count() > 0)
            {
                //every active race is refeshed in a different thread
                //var th = new Thread(() =>

                refreshRaces(activeRaces);

                //th.Start();
                //activeThreads.Add(th);
            }


            //refresh non-active races: whose jumptime-25 hasn't happen 
            var nonActiveRaces = racesToProcess.Where(r => r.ActiveTimeUTC > utcNow).ToList();
            if (nonActiveRaces.Count() > 0)
            {
                //all of them are refresh in one block (more performant)
                refreshRaces(nonActiveRaces);

            }

            //wait until all threads are done
            //activeThreads.ForEach(t => t.Join());
        }


        private void refreshRaces(List<RaceStatus> racesToRefresh)
        {
            List<Meeting> meetings = null;
            List<Race> races = null;
            List<Runner> runners = null;
            List<RaceOdds> odds = null;

            Exception ex = null;
            bool parsed = false;
            try
            {
                //parse races' XMLs
                parseRaces(racesToRefresh, out meetings, out races, out runners, out odds);
                parsed = true;
                //insert/update them in the DB
                updateRaces(meetings, races, runners, odds);
            }
            catch (Exception e)
            {
                Log.Instance.ErrorException(Name + ": failed to " + (parsed ? "update" : "parse") + " races", e);
                racesToRefresh.ToList().ForEach(memRace => memRace.numErrors++);
                ex = e;
            }


            var racesToNotify = new List<RaceStatus>();

            //set jumptime
            racesToRefresh.ToList().ForEach(memRace =>
            {
                var rc = races == null ? null : races.FirstOrDefault(r =>
                    r.MeetingId == memRace.MeetingId && r.RaceNumber == memRace.RaceNumber);

                //if it couldn't be retrieved, we'll add delay til its next retrieval
                if (rc == null || rc.RaceJumpTimeUTC == DateTime.MaxValue)
                {
                    memRace.numErrors++;
                }
                else
                {
                    memRace.numErrors = 0;
                    memRace.IsDone = rc.RaceStatus == ConfigValues.RaceStatusDone
                                  || rc.RaceStatus == ConfigValues.RaceStatusAbandoned;

                    if (memRace.NotInitialized) //first time retrieved
                    {
                        //set active time to jumptime-25
                        memRace.ActiveTimeUTC = rc.RaceJumpTimeUTC.AddSeconds(-ConfigValues.ActiveBeforeJumpTime);
                        //set finishing time to jumptime-5
                        memRace.FinishingTimeUTC = rc.RaceJumpTimeUTC.AddSeconds(-ConfigValues.FinishingBeforeJumpTime);
                    }
                }

                //after 10 errors, an email is sent to the administrator
                if (memRace.numErrors == ConfigValues.NumErrorsToSendEmail ||
                    memRace.numErrors == ConfigValues.NumErrorsToSendEmail2)
                    racesToNotify.Add(memRace);
            });

            //set next refresh
            setNextRefresh(racesToRefresh);

            //store the update time in DB to synchronize with the web app
            DBGateway.SetUpdateTime(racesToRefresh);

            if (racesToNotify.Count > 0)
                sendErrorEmail(racesToNotify, ex);
        }


        private void setNextRefresh(IEnumerable<RaceStatus> races)
        {
            races.ToList().ForEach(r =>
            {
                if (!r.IsDone)
                {
                    if (r.NextRefreshUTC == DateTime.MinValue) //first time retrieved
                        r.NextRefreshUTC = DateTime.UtcNow;

                    //check if race is active
                    var active = r.ActiveTimeUTC <= DateTime.UtcNow;
                    var finishing = r.FinishingTimeUTC <= DateTime.UtcNow;
                    if (active)
                    {
                        if (r.numErrors == 0)
                        {
                            if (finishing) //last 5 minutes, decrese delay
                            {
                                r.NextRefreshUTC = r.NextRefreshUTC.AddSeconds(ConfigValues.FinishingRefreshinterval);
                            }
                            else
                            {
                                r.NextRefreshUTC = r.NextRefreshUTC.AddSeconds(ConfigValues.ActiveRefreshInterval);
                                //if it is after MorningLine-25, set to MorningLine-25
                                if (r.NextRefreshUTC > r.FinishingTimeUTC)
                                    r.NextRefreshUTC = r.FinishingTimeUTC;
                            }
                        }
                        else //there was an error, increase delay with the number of errors
                        {
                            r.NextRefreshUTC = r.NextRefreshUTC.AddSeconds(r.numErrors);
                        }
                    }
                    else
                    {
                        if (r.numErrors == 0)
                        {
                            r.NextRefreshUTC = r.NextRefreshUTC.AddSeconds(ConfigValues.InactiveRefreshInterval);
                            //if it is after MorningLine-25, set to MorningLine-25
                            if (r.NextRefreshUTC > r.ActiveTimeUTC)
                                r.NextRefreshUTC = r.ActiveTimeUTC;
                        }
                        else //there was an error, increase delay with the number of errors
                        {
                            r.NextRefreshUTC = r.NextRefreshUTC.AddSeconds(r.numErrors * r.numErrors * ConfigValues.DelayIfErrorSec);
                        }
                    }

                    Log.Instance.Debug("Next refresh of {0}{1} set for {2}{3}",
                        r.MeetingCode, r.RaceNumber, r.NextRefreshUTC.ToLongTimeString(),
                        active ? (finishing ? " [FINISHING]" : " [ACTIVE]") :
                        r.NotInitialized ? " [NOT INITIALIZED - numErrors=" + r.numErrors + "] " :
                        " [INACTIVE until " + r.ActiveTimeUTC + "]");
                }
                else //is Done
                {
                    Log.Instance.Debug("Race {0}{1} is DONE -> no more refresh", r.MeetingCode, r.RaceNumber);
                }
            });
        }


        private bool loadRaceCodes()
        {
            try
            {
                //race codes are assumed to have been preloaded in DB
                var dbMeetings = DBGateway.GetUpcomingMeetings(CountryCode).ToList();

                Log.Instance.Debug(Name + ": Retrieved "+ dbMeetings.Count() + " upcoming meetings");

                dbMeetings.ForEach(db =>
                {
                    //it is a new meeting --> add all its races
                    for (int i = 0; i < db.NumberOfRaces; i++)
                    {
                        var raceNum = i+1;
                        var memRace = racesStatus.FirstOrDefault(mem => mem.MeetingId == db.MeetingId && mem.RaceNumber == raceNum);
                        if (memRace == null)
                        {
                            //create one entry per race
                            racesStatus.Add(new RaceStatus
                            {
                                MeetingId = db.MeetingId,
                                MeetingCode = db.MeetingCode,
                                DateUTC = db.MeetingDate,
                                AUS_StateId = db.AUS_StateId,
                                RaceNumber = raceNum
                            });
                        }
                        else
                        {
                            //update existing entry
                            //memRace.MeetingId = db.MeetingId;
                            memRace.MeetingCode = db.MeetingCode;
                            memRace.DateUTC = db.MeetingDate;
                            memRace.AUS_StateId = db.AUS_StateId;
                            //memRace.RaceNumber = raceNum;
                        }
                    }
                });

                //unload old races from memory: those that are not being retrieved from db anymore and are old
                racesStatus.RemoveAll(mem => !dbMeetings.Exists(db => db.MeetingId == mem.MeetingId)
                                           && mem.DateUTC < DateTime.UtcNow.Date);
                
                //in HK, jump times are not in the XMLs, so they are retrieved from DB
                retrieveJumpTimes();

                return true;

            }
            catch (Exception e)
            {
                Log.Instance.ErrorException(Name + ": failed to load upcoming meetings from DB", e);
                return false;
            }
        }


        private void retrieveJumpTimes()
        {
            List<int> meetingIds = new List<int>();
            racesStatus.ForEach(r =>
            {
                if (!meetingIds.Contains(r.MeetingId))
                    meetingIds.Add(r.MeetingId);
            });

            if (meetingIds.Count() > 0)
            {
                var jumpTimes = DBGateway.GetJumpTimes(meetingIds);

                racesStatus.ForEach(r =>
                    {
                        var jtLocal = jumpTimes.FirstOrDefault(jt => jt.MeetingId == r.MeetingId && jt.RaceNumber == r.RaceNumber);
                        if (jtLocal != null && jtLocal.LocalJumpTime != null)
                        {
                            DateTime dt = r.DateUTC + jtLocal.LocalJumpTime;
                            r.JumpTimeUTC = TimeZoneHelper.ToUTC(dt, r.AUS_StateId);
                        }
                    });
            }
        }


        private void sendErrorEmail(List<RaceStatus> racesToNotify, Exception ex)
        {
            try
            {
                var msg = new StringBuilder();
                msg.AppendFormat( @"<p>Hi Simon!</p>
                                    <p>This is an automatically generated email</p>
                                    <p>The following {0} races have failed to be retrieved/parsed for {1} consecutive times:</p>                                    
                                    <ul>", Name, ConfigValues.NumErrorsToSendEmail);

                racesToNotify.ForEach(r =>
                    {
                        msg.AppendFormat("<li>MeetingDate: {0}<br/>MeetingId: {1}<br/>RaceNo: {2}<br/>MeetingCode: {3}<br/>AUS_StateId: {4}<br/>Jump time: {5}</li>", 
                            r.DateUTC.ToShortDateString(), r.MeetingId, r.RaceNumber, 
                            r.MeetingCode ?? "<b>NULL <===</b>", 
                            r.AUS_StateId != 0 ? r.AUS_StateId.ToString() : "<b>0 <===</b>",
                            r.JumpTimeUTC != DateTime.MaxValue ? r.JumpTimeUTC.ToShortTimeString() + "UTC" : "<b>NOT SET</b> <===");
                    });

                msg.Append("</ul>");

                if (ex != null)
                    msg.AppendFormat("<p>The application raised the following exception:</p><p>{0}</p>", ex.ToString());

                msg.Append("<p>Can you please have a look?</p><p>Thanks!</p>");


                var mail = new System.Net.Mail.MailMessage();
                mail.Subject = "Recurrent error on XMLFeedParser app";
                mail.Body = msg.ToString();
                mail.IsBodyHtml = true;
                //mail.From = new MailAddress(From);
                mail.To.Add(ConfigValues.AdminEmails);

                var client = new System.Net.Mail.SmtpClient();
                client.Host = "smtp.gmail.com";
                //client.EnableSsl = true;
                client.Send(mail);

                Log.Instance.Debug("Notification email sent");
            }
            catch (Exception e)
            {
                Log.Instance.ErrorException(Name + ": failed to send error email", e);
            }
        }

    }
}
