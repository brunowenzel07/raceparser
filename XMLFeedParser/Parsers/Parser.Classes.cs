using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMLFeedParser.Model;

namespace XMLFeedParser.Parsers
{
    abstract partial class Parser
    {
        protected class RaceStatus
        {
            //these fields are retrieved from DB
            public int MeetingId;
            public DateTime DateUTC;
            public string MeetingCode;
            public int RaceNumber;
            public int AUS_StateId;

            //these fields are retrieved from XML or calculated
            public bool IsDone;

            public DateTime JumpTimeUTC = DateTime.MaxValue; //this value is only used by HKG
            public DateTime ActiveTimeUTC = DateTime.MaxValue;
            public DateTime FinishingTimeUTC = DateTime.MaxValue;
            public DateTime NextRefreshUTC = DateTime.MinValue; //to force first refresh
            public int numErrors = 0;

            public bool NotInitialized { get { return ActiveTimeUTC == DateTime.MaxValue; } }
        }

    }
}
