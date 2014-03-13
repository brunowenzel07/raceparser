using System.Configuration;

namespace XMLFeedParser.Config
{
    public static class ConfigValues
    {
        public const string RaceStatusAlive = "SELLING";
        public const string RaceStatusDone = "PAYING";
        public const string RaceStatusAbandoned = "ABANDONED";

        public static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["RaceDayDB"].ConnectionString;

        public static readonly int ThreadsSleepTime = int.Parse(ConfigurationManager.AppSettings["ThreadSleepIntervalMs"]);
        public static readonly int UpcomingMeetingsRefreshInterval = int.Parse(ConfigurationManager.AppSettings["UpcomingMeetings_RefreshIntervalSec"]);

        public static readonly int InactiveRefreshInterval = int.Parse(ConfigurationManager.AppSettings["Inactive_RefreshIntervalSec"]);
        public static readonly int ActiveRefreshInterval = int.Parse(ConfigurationManager.AppSettings["Active_RefreshIntervalSec"]);
        public static readonly int FinishingRefreshinterval = int.Parse(ConfigurationManager.AppSettings["Finishing_RefreshintervalSec"]);
        public static readonly int ActiveBeforeJumpTime = int.Parse(ConfigurationManager.AppSettings["Active_BeforeJumpTimeSec"]);
        public static readonly int FinishingBeforeJumpTime = int.Parse(ConfigurationManager.AppSettings["Finishing_BeforeJumpTimeSec"]);
        
        public static readonly int SecsInactiveForRaceDone = int.Parse(ConfigurationManager.AppSettings["SecsInactiveForRaceDone"]);

        public static readonly string AdminEmails = ConfigurationManager.AppSettings["AdminEmails"];

        public const string YES = "Y";

        public const int DelayIfErrorSec = 10;

        public static int NumErrorsToSendEmail = 10;
        public static int NumErrorsToSendEmail2 = 50;
    }
}
