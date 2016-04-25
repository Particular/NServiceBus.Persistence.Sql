using System.Xml.Serialization;

static class EmptyXmlNamespace
{
    public static readonly XmlSerializerNamespaces EmptyNamespace;

    static EmptyXmlNamespace()
    {
        EmptyNamespace = new XmlSerializerNamespaces();
        EmptyNamespace.Add("", "");
    }
}