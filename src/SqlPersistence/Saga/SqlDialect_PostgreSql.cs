namespace NServiceBus
{
    using System.Text;

    public partial class SqlDialect
    {
        public partial class PostgreSql
        {
            internal override string GetSagaTableName(string tablePrefix, string tableSuffix)
            {
                return $"{tablePrefix}{tableSuffix}";
            }

            internal override string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName)
            {
                var valuesBuilder = new StringBuilder();
                var insertBuilder = new StringBuilder();

                if (correlationProperty != null)
                {
                    insertBuilder.Append($",\r\n    \"Correlation_{correlationProperty}\"");
                    valuesBuilder.Append(",\r\n    @CorrelationId");
                }
                if (transitionalCorrelationProperty != null)
                {
                    insertBuilder.Append($",\r\n    \"Correlation_{transitionalCorrelationProperty}\"");
                    valuesBuilder.Append(",\r\n    @TransitionalCorrelationId");
                }

                return $@"
insert into public.""{tableName}""
(
    ""Id"",
    ""Metadata"",
    ""Data"",
    ""PersistenceVersion"",
    ""SagaTypeVersion"",
    ""Concurrency""{insertBuilder}
)
values
(
    @Id,
    @Metadata,
    @Data,
    @PersistenceVersion,
    @SagaTypeVersion,
    1{valuesBuilder}
)";
            }


            internal override string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName)
            {
                // no need to set CorrelationProperty since it is immutable
                var correlationSet = "";
                if (transitionalCorrelationProperty != null)
                {
                    correlationSet = $",\r\n    Correlation_{transitionalCorrelationProperty} = @TransitionalCorrelationId";
                }

                return $@"
update public.""{tableName}""
set
    ""Data"" = @Data,
    ""Metadata"" = @Metadata,
    ""PersistenceVersion"" = @PersistenceVersion,
    ""SagaTypeVersion"" = @SagaTypeVersion,
    ""Concurrency"" = @Concurrency + 1{correlationSet}
where
    ""Id"" = @Id and ""Concurrency"" = @Concurrency
";
            }

            internal override string BuildGetBySagaIdCommand(string tableName)
            {
                return $@"
select
    ""Id"",
    ""SagaTypeVersion"",
    ""Concurrency"",
    ""Metadata"",
    ""Data""
from public.""{tableName}""
where ""Id"" = @Id
";
            }

            internal override string BuildGetByPropertyCommand(string propertyName, string tableName)
            {
                return $@"
select
    ""Id"",
    ""SagaTypeVersion"",
    ""Concurrency"",
    ""Metadata"",
    ""Data""
from public.""{tableName}""
where ""Correlation_{propertyName}"" = @propertyValue
";
            }

            internal override string BuildCompleteCommand(string tableName)
            {
                return $@"
delete from public.""{tableName}""
where ""Id"" = @Id and ""Concurrency"" = @Concurrency
";
            }

            internal override string BuildSelectFromCommand(string tableName)
            {
                return $@"
select
    ""Id"",
    ""SagaTypeVersion"",
    ""Concurrency"",
    ""Metadata"",
    ""Data""
from public.""{tableName}""
";
            }
        }
    }
}