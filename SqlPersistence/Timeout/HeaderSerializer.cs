using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

static class HeaderSerializer
{

    public static string ToXml(Dictionary<string, string> dictionary)
    {
        var xElem = new XElement(
                      "headers",
                      dictionary.Select(BuildHeaderElement)
                   );
        return xElem.ToString();
    }

    static XElement BuildHeaderElement(KeyValuePair<string, string> header)
    {
        return new XElement("header", new XAttribute("key", header.Key), header.Value);
    }

    public static Dictionary<string, string> FromString(string xml)
    {
        return XElement.Parse(xml)
            .Elements("header")
            .ToDictionary(
                el => (string) el.Attribute("key"),
                el => el.Value
            );
    }
}