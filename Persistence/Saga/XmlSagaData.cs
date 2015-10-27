using System;
using System.Xml.Serialization;

namespace NServiceBus.SqlPersistence
{
    public class XmlSagaData : IContainSagaData
    {
        [XmlIgnore]
        public Guid Id { get; set; }
        [XmlIgnore]
        public string Originator { get; set; }
        [XmlIgnore]
        public string OriginalMessageId { get; set; }
    }
}