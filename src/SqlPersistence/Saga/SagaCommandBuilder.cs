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
        public string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName)
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
    @Id,
    @Metadata,
    @Data,
    @PersistenceVersion,
    @SagaTypeVersion,
    1{valuesBuilder}
)";
        }


        public string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName)
        {
            // no need to set CorrelationProperty since it is immutable
            var correlationSet = "";
            if (transitionalCorrelationProperty != null)
            {
                correlationSet = $",\r\n    Correlation_{transitionalCorrelationProperty} = @TransitionalCorrelationId";
            }

            return $@"
update {tableName}
set
    Data = @Data,
    PersistenceVersion = @PersistenceVersion,
    SagaTypeVersion = @SagaTypeVersion,
    Concurrency = @Concurrency + 1{correlationSet}
where
    Id = @Id AND Concurrency = @Concurrency
";
        }

        public string BuildGetBySagaIdCommand(string tableName)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
where Id = @Id
";
        }

        public string BuildGetByPropertyCommand(string propertyName, string tableName)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
where Correlation_{propertyName} = @propertyValue
";
        }

        public string BuildCompleteCommand(string tableName)
        {
            return $@"
delete from {tableName}
where Id = @Id AND Concurrency = @Concurrency
";
        }
    }
}