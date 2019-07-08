using System.Xml.Serialization;

namespace GSD.Service.UI.Data
{
    public class ActionItem
    {
        [XmlElement("content")]
        public string Content { get; set; }

        [XmlElement("arguments")]
        public string Arguments { get; set; }
    }
}