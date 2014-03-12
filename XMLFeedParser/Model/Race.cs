using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLFeedParser.Model
{
    public abstract class Race
    {
        public int MeetingId;
        public int RaceNumber;
        public string RaceStatus;
        public DateTime RaceJumpTimeUTC;
    }

    public class RaceTatts: Race
    {
        public string RaceName;
        public string DistanceName;
        public int TrackRating;
        public bool isTrackChanged; 
    }

    public class RaceHKG: Race
    {
        public double RaceWinPool;
        public double RacePPPool;
    }

}
