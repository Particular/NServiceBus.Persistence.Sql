using System;

namespace NServiceBus.Persistence.Sql.Xml
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SqlSagaAttribute : Attribute
    {
        public string CorrelationId;
        public string TransitionalCorrelationId;
        public string TableName;

        public SqlSagaAttribute(string correlationId, string transitionalCorrelationId = null, string tableName = null)
        {
            CorrelationId = correlationId;
            TransitionalCorrelationId = transitionalCorrelationId;
            TableName = tableName;
            Guard.AgainstNullAndEmpty(nameof(correlationId), correlationId);
            Guard.AgainstEmpty(nameof(transitionalCorrelationId), transitionalCorrelationId);
            Guard.AgainstEmpty(nameof(tableName), tableName);
        }
    }
}