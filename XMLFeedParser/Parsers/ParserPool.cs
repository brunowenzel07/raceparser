using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using XMLFeedParser.Config;
using XMLFeedParser.Model;

namespace XMLFeedParser.Parsers
{
    internal class ParserPool
    {
        //List<IParser> parsers = new List<IParser>();
        public static bool Running = true;


        public ParserPool()
        {
        }

        public void Add(string name, string className, string baseUrl, string countryCode)
        {
            Parser p = (Parser)Activator.CreateInstance(Type.GetType(className));
            p.Name = name;
            p.BaseUrl = baseUrl;
            p.CountryCode = countryCode;
            //parsers.Add(p);

            Thread thread = new Thread(() => ThreadJob(p));
            thread.Start();
        }

        static void ThreadJob(Parser p)
        {
            var sleepTime = ConfigValues.ThreadsSleepTime;
            var numErrors = 0;

            while (Running)
            {
                try
                {
                    p.Dojob();
                    Thread.Sleep(sleepTime);
                }
                catch (Exception e)
                {
                    Log.Instance.ErrorException("Error on " + p.Name + " parser main loop", e);

                    //to prevent a potential log overflow, every 10 errors, sleepTime is increased
                    if (++numErrors % 10 == 0)
                    {
                        sleepTime *= 2;
                        Log.Instance.Warn(p.Name + " parser sleep time increased to " + sleepTime + " ms after " + numErrors + " errors");
                    }
                }
            }
        }
    }
}
