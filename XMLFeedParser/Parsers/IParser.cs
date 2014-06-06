using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLFeedParser.Parsers
{
    interface IParser
    {
        string BaseUrl { get; set; }
        string Name { get; set; }
        string CountryCode { get; set; }

        void Dojob();

    }
}
