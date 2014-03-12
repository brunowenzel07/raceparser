using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLFeedParser.Model
{
    public class RaceOdds
    {
	    public int MeetingId;
	    public int RaceNumber;
	    public float EXPoolTotal;
	    public float EXDivAmount;
	    public float F4PoolTotal; 
	    public float F4DivAmount; 
	    public float QNPoolTotal;
        public float QNDivAmount;
	    public float TFDivAmount; 
	    public float TFPoolTotal; 
	    public float WWDivAmount; 
	    public float WWPoolTotal; 
	    public float PPDivAmount; 
	    public float PPPoolTotal;
        public string RaceStatus;
    }
}
