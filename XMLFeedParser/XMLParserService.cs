using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using XMLFeedParser.Config;
using XMLFeedParser.Parsers;

namespace XMLFeedParser
{
    public partial class XMLParserService : ServiceBase
    {
        public XMLParserService()
        {
            InitializeComponent();
        }

        public void Start()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                ParserPool parserPool = new ParserPool();

                ParserSection config = (ParserSection)System.Configuration.ConfigurationManager.GetSection("parsers");
                foreach (var parserConfig in config.Instances)
                {
                    var cfg = (ParserInstanceElement)parserConfig;
                    if (cfg.Enabled)
                    {
                        try
                        {
                            parserPool.Add(cfg.Name, cfg.Class, cfg.BaseUrl, cfg.Country);
                        }
                        catch (Exception e)
                        {
                            Log.Instance.ErrorException("Could not initilialize " + cfg.Name + " parser", e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Instance.FatalException("Could not start service", e);
            }
        }

        protected override void OnStop()
        {
            ParserPool.Running = false; //stops all threads
        }
    }
}
