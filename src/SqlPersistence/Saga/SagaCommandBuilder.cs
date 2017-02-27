using System.Text;
using System;

namespace NServiceBus.Persistence.Sql
{

    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public class SagaCommandBuilder
    {
        string tablePrefix;

        public SagaCommandBuilder(SqlVariant sqlVariant, string tablePrefix)
        {
            this.tablePrefix = tablePrefix;
        }

        public string BuildSaveCommand(string tableSuffix, string correlationProperty, string transitionalCorrelationProperty)
        {
            var valuesBuilder = new StringBuilder();
            var insertBuilder = new StringBuilder();

            if (correlationProperty != null)
            {
                insertBuilder.Append($",\r\n    Correlation_{correlationProperty}");
                valuesBuilder.Append(",\r\n    @CorrelationId");
            }
            if (transitionalCorrelationProperty != null)
            {
                insertBuilder.Append($",\r\n    Correlation_{transitionalCorrelationProperty}");
                valuesBuilder.Append(",\r\n    @TransitionalCorrelationId");
            }

            return $@"
insert into {tablePrefix}{tableSuffix}
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
    @Id,
    @Metadata,
    @Data,
    @PersistenceVersion,
    @SagaTypeVersion,
    1{valuesBuilder}
)";
        }


        public string BuildUpdateCommand(string tableSuffix, string transitionalCorrelationProperty)
        {
            // no need to set CorrelationProperty since it is immutable
            var correlationSet = "";
            if (transitionalCorrelationProperty != null)
            {
                correlationSet = $",\r\n    Correlation_{transitionalCorrelationProperty} = @TransitionalCorrelationId";
            }

            return $@"
update {tablePrefix}{tableSuffix}
set
    Data = @Data,
    PersistenceVersion = @PersistenceVersion,
    SagaTypeVersion = @SagaTypeVersion,
    Concurrency = @Concurrency + 1{correlationSet}
where
    Id = @Id AND Concurrency = @Concurrency
";
        }

        public string BuildGetBySagaIdCommand(string tableSuffix)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tablePrefix}{tableSuffix}
where Id = @Id
";
        }

        public string BuildGetByPropertyCommand(string tableSuffix, string propertyName)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tablePrefix}{tableSuffix}
where Correlation_{propertyName} = @propertyValue
";
        }

        public string BuildCompleteCommand(string tableSuffix)
        {
            return $@"
delete from {tablePrefix}{tableSuffix}
where Id = @Id AND Concurrency = @Concurrency
";
        }
    }
}