namespace NServiceBus
{
    using System;
    using System.Text;

    public partial class SqlDialect
    {
        public partial class Oracle
        {
            internal override string GetSagaTableName(string tablePrefix, string tableSuffix)
            {
                if (tableSuffix.Length > 27)
                {
                    throw new Exception($"Saga '{tableSuffix}' contains more than 27 characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, shorten the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
                }
                if (Encoding.UTF8.GetBytes(tableSuffix).Length != tableSuffix.Length)
                {
                    throw new Exception($"Saga '{tableSuffix}' contains non-ASCII characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, change the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
                }
                return tableSuffix.ToUpper();
            }

            internal override string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName)
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
insert into {TableName(tableName)}
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

            internal override string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName)
            {
                // no need to set CorrelationProperty since it is immutable
                var correlationSet = "";
                if (transitionalCorrelationProperty != null)
                {
                    correlationSet = $",\r\n    {CorrelationPropertyName(transitionalCorrelationProperty)} = :TransitionalCorrelationId";
                }

                return $@"
update {TableName(tableName)}
set
    Data = :Data,
    PersistenceVersion = :PersistenceVersion,
    SagaTypeVersion = :SagaTypeVersion,
    Concurrency = :Concurrency + 1{correlationSet}
where
    Id = :Id and Concurrency = :Concurrency
";
            }

            internal override string BuildGetBySagaIdCommand(string tableName)
            {
                return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {TableName(tableName)}
where Id = :Id
";
            }

            internal override string BuildGetByPropertyCommand(string propertyName, string tableName)
            {
                return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {TableName(tableName)}
where {CorrelationPropertyName(propertyName)} = :propertyValue
";
            }

            internal override string BuildCompleteCommand(string tableName)
            {
                return $@"
delete from {TableName(tableName)}
where Id = :Id and Concurrency = :Concurrency
";
            }

            internal override string BuildSelectFromCommand(string tableName)
            {
                return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {TableName(tableName)}
";
            }

            string CorrelationPropertyName(string propertyName)
            {
                var oracleName = "CORR_" + propertyName.ToUpper();
                return oracleName.Length > 30 ? oracleName.Substring(0, 30) : oracleName;
            }

            string TableName(string name)
            {
                return $"\"{name.ToUpper()}\"";
            }

        }
    }
}