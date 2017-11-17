#pragma warning disable 672 // overrides obsolete
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace NServiceBus
{
    using System;
    using System.Text;

    public partial class SqlDialect
    {
        public partial class Oracle
        {
            public override string GetSagaTableName(string tablePrefix, string tableSuffix)
            {
                if (tableSuffix.Length > 27)
                {
                    throw new Exception($"Saga '{tableSuffix}' contains more than 27 characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, shorten the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
                }
                if (Encoding.UTF8.GetBytes(tableSuffix).Length != tableSuffix.Length)
                {
                    throw new Exception($"Saga '{tableSuffix}' contains non-ASCII characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, change the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
                }
                return $"{SchemaPrefix}\"{tableSuffix.ToUpper()}\"";
            }

            public override string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName)
            {
                var valuesBuilder = new StringBuilder();
                var insertBuilder = new StringBuilder();

                if (correlationProperty != null)
                {
                    insertBuilder.Append($",\r\n    {CorrelationPropertyName(correlationProperty)}");
                    valuesBuilder.Append(",\r\n    :CorrelationId");
                }
                if (transitionalCorrelationProperty != null)
                {
                    insertBuilder.Append($",\r\n    {CorrelationPropertyName(transitionalCorrelationProperty)}");
                    valuesBuilder.Append(",\r\n    :TransitionalCorrelationId");
                }

                return $@"
insert into {tableName}
(
    Id,
    Metadata,
    Data,
    PersistenceVersion,
    SagaTypeVersion,
    Concurrency{insertBuilder}
)
values
(
    :Id,
    :Metadata,
    :Data,
    :PersistenceVersion,
    :SagaTypeVersion,
    1{valuesBuilder}
)";
            }

            public override string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName)
            {
                // no need to set CorrelationProperty since it is immutable
                var correlationSet = "";
                if (transitionalCorrelationProperty != null)
                {
                    correlationSet = $",\r\n    {CorrelationPropertyName(transitionalCorrelationProperty)} = :TransitionalCorrelationId";
                }

                return $@"
update {tableName}
set
    Data = :Data,
    PersistenceVersion = :PersistenceVersion,
    SagaTypeVersion = :SagaTypeVersion,
    Concurrency = :Concurrency + 1{correlationSet}
where
    Id = :Id and Concurrency = :Concurrency
";
            }

            public override string BuildGetBySagaIdCommand(string tableName)
            {
                return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
where Id = :Id
";
            }

            public override string BuildGetByPropertyCommand(string propertyName, string tableName)
            {
                return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
where {CorrelationPropertyName(propertyName)} = :propertyValue
";
            }

            public override string BuildCompleteCommand(string tableName)
            {
                return $@"
delete from {tableName}
where Id = :Id and Concurrency = :Concurrency
";
            }

            public override string BuildSelectFromCommand(string tableName)
            {
                return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
";
            }

            string CorrelationPropertyName(string propertyName)
            {
                var oracleName = "CORR_" + propertyName.ToUpper();
                return oracleName.Length > 30 ? oracleName.Substring(0, 30) : oracleName;
            }
        }
    }
}