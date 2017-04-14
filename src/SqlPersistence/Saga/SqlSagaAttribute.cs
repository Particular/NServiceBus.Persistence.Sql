using System;
#pragma warning disable 1591

namespace NServiceBus.Persistence.Sql
{
    [ObsoleteEx(
        Message = @"Replaced by overrides on SqlSaga.
 * For correlationProperty override CorrelationPropertyName on the saga implementing SqlSaga<T>.
 * For transitionalCorrelationProperty override TransitionalCorrelationPropertyName on the saga implementing SqlSaga<T>
 * For tableSuffix override TableSuffix on the saga implementing SqlSaga<T>",
        RemoveInVersion = "3.0",
        TreatAsErrorFromVersion = "2.0")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SqlSagaAttribute : Attribute
    {
        public SqlSagaAttribute(
            string correlationProperty = null,
            string transitionalCorrelationProperty = null,
            string tableSuffix = null)
        {
        }
    }
}