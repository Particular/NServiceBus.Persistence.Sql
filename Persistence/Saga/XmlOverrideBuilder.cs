using System;
using System.Xml.Serialization;
using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence
{

    public static class XmlOverrideBuilder
    {
        public static readonly XmlSerializerNamespaces EmptyNamespace;

        static XmlOverrideBuilder()
        {
            EmptyNamespace = new XmlSerializerNamespaces();
            EmptyNamespace.Add("", "");
        }

        public static XmlAttributeOverrides BuildAttributeOverrides(Type sagaDataType)
        {
            var overrides = new XmlAttributeOverrides();
            var ignore = new XmlAttributes {XmlIgnore = true};

            var overrideType = sagaDataType;
            if (typeof (ContainSagaData).IsAssignableFrom(sagaDataType))
            {
                overrideType = typeof (ContainSagaData);
            }
            overrides.Add(overrideType, "OriginalMessageId", ignore);
            overrides.Add(overrideType, "Originator", ignore);
            overrides.Add(overrideType, "Id", ignore);
            return overrides;
        }
    }
}