using System;

namespace NServiceBus.SqlPersistence
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CorrelationIdAttribute : Attribute
    {
    }
}