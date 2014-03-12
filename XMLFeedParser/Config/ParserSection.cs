
using System.Configuration;

namespace XMLFeedParser.Config
{
    public class ParserSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public ParserInstanceCollection Instances
        {
            get { return (ParserInstanceCollection)this[""]; }
            set { this[""] = value; }
        }
    }
    public class ParserInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ParserInstanceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            //set to whatever Element Property you want to use for a key
            return ((ParserInstanceElement)element).Name;
        }
    }

    public class ParserInstanceElement : ConfigurationElement
    {
        //Make sure to set IsKey=true for property exposed as the GetElementKey above
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("class", IsRequired = true)]
        public string Class
        {
            get { return (string)base["class"]; }
            set { base["class"] = value; }
        }

        [ConfigurationProperty("baseUrl", IsRequired = true)]
        public string BaseUrl
        {
            get { return (string)base["baseUrl"]; }
            set { base["baseUrl"] = value; }
        }

        [ConfigurationProperty("country", IsRequired = true)]
        public string Country
        {
            get { return (string)base["country"]; }
            set { base["country"] = value; }
        }

        [ConfigurationProperty("enabled", IsRequired = true)]
        public bool Enabled
        {
            get { return (bool)base["enabled"]; }
            set { base["enabled"] = value; }
        }

    }
}
