using System.Xml.Serialization;

namespace GSD.Service.UI.Data
{
    public class VisualData
    {
        [XmlElement("binding")]
        public BindingData Binding { get; set; }
    }
}
