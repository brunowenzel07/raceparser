using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XMLFeedParser
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var ServiceToRun = new XMLParserService();
            if (Environment.UserInteractive)
            {
                // This used to run the service as a console (development phase only)

                ServiceToRun.Start();

                Console.WriteLine("Press Enter to terminate ...");
                Console.ReadLine();

                ServiceToRun.Stop();
            }
            else
            {
                ServiceBase.Run(ServiceToRun);
            }
        }
    }
}
