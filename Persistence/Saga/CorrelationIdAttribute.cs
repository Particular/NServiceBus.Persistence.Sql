using System;

namespace NServiceBus.SqlPersistence
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CorrelationIdAttribute : Attribute
    {
    }
}