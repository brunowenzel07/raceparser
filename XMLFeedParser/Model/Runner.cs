using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLFeedParser.Model
{
    public abstract class Runner
    {
        public int MeetingId;
        public int RaceNumber;
        public int HorseNumber;
        public bool isScratched;
        public double WinOdds;
        public double Placeodds;
    }

    public class RunnerTatts: Runner
    {
        public string HorseName;
        public string JockeyName;
        public string Barrier;
        public string AUS_HcpWt;
        public bool IsFavourite;
        public bool isJockeyChanged;
	    public int Place;
    }

    public class RunnerHKG: Runner
    {
        public bool IsWinFavourite;
        public bool WinDropBy20;
        public bool WinDropBy50;
        public bool IsPlaceFavourite;
        public bool PlaceDropBy20;
        public bool PlaceDropBy50;
    }


}
