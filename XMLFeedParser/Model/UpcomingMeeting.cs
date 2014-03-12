using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLFeedParser.Model
{
    public class UpcomingMeeting
    {
        public int MeetingId;
        public DateTime MeetingDate;
        public string MeetingCode;
        public int NumberOfRaces;
        public int AUS_StateId;

        public static IEnumerable<UpcomingMeeting> DummyList(string countryCode)
        { 
            var l = new List<UpcomingMeeting>();
            if (countryCode == "AUS")
            {
                l.Add(new UpcomingMeeting
                {
                    MeetingId = 1,
                    MeetingDate = DateTime.Parse("2014/03/02"),
                    MeetingCode = "NR",
                    NumberOfRaces = 7,
                    AUS_StateId = 21
                });

                //l.Add(new UpcomingMeeting
                //{
                //    MeetingId = 1,
                //    MeetingDate = DateTime.UtcNow.Date,
                //    MeetingCode = "NR",
                //    NumberOfRaces = 7,
                //    AUS_StateId = 24
                //});

                //l.Add(new UpcomingMeeting
                //{
                //    MeetingId = 2,
                //    MeetingDate = DateTime.UtcNow.Date,
                //    MeetingCode = "VR",
                //    NumberOfRaces = 8,
                //    AUS_StateId = 25
                //});

                //l.Add(new UpcomingMeeting
                //{
                //    MeetingId = 3,
                //    MeetingDate = DateTime.UtcNow.Date,
                //    MeetingCode = "CR",
                //    NumberOfRaces = 7,
                //    AUS_StateId = 19
                //});

                //l.Add(new UpcomingMeeting
                //{
                //    MeetingId = 4,
                //    MeetingDate = DateTime.UtcNow.Date,
                //    MeetingCode = "BT",
                //    NumberOfRaces = 7,
                //    AUS_StateId = 21
                //});
            }
            else
            {
                l.Add(new UpcomingMeeting
                {
                    MeetingId = 5,
                    MeetingDate = DateTime.UtcNow.Date.Subtract(new TimeSpan(1,0,0,0,0)),
                    MeetingCode = "HV",
                    NumberOfRaces = 2,
                    AUS_StateId = 29
                });
            }

            return l;
        }
    }
}
