using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLFeedParser
{
    internal static class Log
    {
        public static Logger Instance { get; private set; }
        static Log()
        {
            LogManager.ThrowExceptions = true;

//TODO remove sentinel loggin when deploying final version
//#if DEBUG
            // Setup the logging view for Sentinel - http://sentinel.codeplex.com
            var sentinalTarget = new NLogViewerTarget()
            {
                Name = "sentinal",
                Address = "udp://127.0.0.1:9999"
            };
            var sentinalRule = new LoggingRule("*", LogLevel.Trace, sentinalTarget);
            LogManager.Configuration.AddTarget("sentinal", sentinalTarget);
            LogManager.Configuration.LoggingRules.Add(sentinalRule);

            // Setup the logging view for Harvester - http://harvester.codeplex.com
            //var harvesterTarget = new OutputDebugStringTarget()
            //{
            //    Name = "harvester",
            //    Layout = new Log4JXmlEventLayout()
            //};
            //var harvesterRule = new LoggingRule("*", LogLevel.Trace, harvesterTarget);
            //LogManager.Configuration.AddTarget("harvester", harvesterTarget);
            //LogManager.Configuration.LoggingRules.Add(harvesterRule);
//#endif

            LogManager.ReconfigExistingLoggers();

            Instance = LogManager.GetCurrentClassLogger();
        }
    }
}
