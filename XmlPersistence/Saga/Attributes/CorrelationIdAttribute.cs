using System;

namespace NServiceBus.Persistence.Sql.Xml
{
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
    public class CorrelationIdAttribute : Attribute
    {
    }
}