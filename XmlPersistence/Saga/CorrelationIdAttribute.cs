using System;

namespace NServiceBus.Persistence.SqlServerXml
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CorrelationIdAttribute : Attribute
    {
    }
}