using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

static class HeaderSerializer
{
    static XmlSerializer serializer;
    static XmlSerializerNamespaces emptyNamespace;

    static HeaderSerializer()
    {

        emptyNamespace = new XmlSerializerNamespaces();
        emptyNamespace.Add("", "");
        serializer = new XmlSerializer(
            type: typeof (List<Header>),
            root: new XmlRootAttribute("Headers"));

    }


    public static string ToXml(Dictionary<string, string> dictionary)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            var headers = dictionary.Select(x => new Header
            {
                Key = x.Key,
                Value = x.Value
            }).ToList();
            serializer.Serialize(writer, headers, emptyNamespace);
        }
        return builder.ToString();
    }

    public static Dictionary<string, string> FromString(string value)
    {
        using (var reader = new StringReader(value))
        {
            var list = (List<Header>)serializer.Deserialize(reader);
            return list.ToDictionary(header => header.Key, header => header.Value);
        }
    }
}