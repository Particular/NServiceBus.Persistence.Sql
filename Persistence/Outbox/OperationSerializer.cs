using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NServiceBus.Outbox;

static class OperationSerializer
{

    public static string ToXml(IEnumerable<TransportOperation> operations)
    {
        var xElem = new XElement(
                      "operations",
                       operations.Select(BuildOperationElement)
                   );
        return xElem.ToString();
    }

    static XElement BuildOperationElement(TransportOperation operation)
    {
        return new XElement("operation",
            new XAttribute("messageId", operation.MessageId),
            new XElement(
                "headers",
                operation.Headers.Select(BuildHeaderElement)
                ),
            new XElement(
                "options",
                operation.Options.Select(BuildOptionElement)
                ),
            new XElement("body", Convert.ToBase64String(operation.Body)));
    }

    static XElement BuildHeaderElement(KeyValuePair<string, string> header)
    {
        return new XElement("header", new XAttribute("key", header.Key), header.Value);
    }
    static XElement BuildOptionElement(KeyValuePair<string, string> header)
    {
        return new XElement("option", new XAttribute("key", header.Key), header.Value);
    }

    public static IEnumerable<TransportOperation> FromString(string xml)
    {
        return XElement.Parse(xml)
            .Elements("operation")
            .Select(ElementToOperation);
    }

    static TransportOperation ElementToOperation(XElement transportElement)
    {
        return new TransportOperation(
            messageId: (string) transportElement.Attribute("messageId"),
            headers: ReadHeaders(transportElement),
            options: ReadOptions(transportElement),
            body: Convert.FromBase64String(transportElement.Element("body").Value));
    }

    static Dictionary<string, string> ReadOptions(XElement transportElement)
    {
        var xElement = transportElement.Element("options");
        return xElement
            .Elements("option")
            .ToDictionary(
            optionElement => (string)optionElement.Attribute("key"),
            optionElement => optionElement.Value
            );
    }

    static Dictionary<string, string> ReadHeaders(XElement transportElement)
    {
        return transportElement.Element("headers")
            .Elements("header")
            .ToDictionary(
            optionElement => (string)optionElement.Attribute("key"),
            optionElement => optionElement.Value
            );
    }
}