using System.Text;

namespace NServiceBus.Persistence.Sql
{
    public class SagaCommandBuilder
    {
        string tablePrefix;

        public SagaCommandBuilder(SqlVariant sqlVariant, string tablePrefix)
        {
            this.tablePrefix = tablePrefix;
        }

        public string BuildSaveCommand(string tableSuffx, string correlationProperty, string transitionalCorrelationProperty)
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
insert into {tablePrefix}{tableSuffx}
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


        public string BuildUpdateCommand(string tableSuffx, string transitionalCorrelationProperty)
        {
            // no need to set CorrelationProperty since it is immutable
            var correlationSet = "";
            if (transitionalCorrelationProperty != null)
            {
                correlationSet = $",\r\n    Correlation_{transitionalCorrelationProperty} = @TransitionalCorrelationId";
            }

            return $@"
update {tablePrefix}{tableSuffx}
set
    Data = @Data,
    PersistenceVersion = @PersistenceVersion,
    SagaTypeVersion = @SagaTypeVersion,
    Concurrency = @Concurrency + 1{correlationSet}
where
    Id = @Id AND Concurrency = @Concurrency
";
        }

        public string BuildGetBySagaIdCommand(string tableSuffx)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tablePrefix}{tableSuffx}
where Id = @Id
";
        }

        public string BuildGetByPropertyCommand(string tableSuffx, string propertyName)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tablePrefix}{tableSuffx}
where Correlation_{propertyName} = @propertyValue
";
        }

        public string BuildCompleteCommand(string tableSuffx)
        {
            return $@"
delete from {tablePrefix}{tableSuffx}
where Id = @Id AND Concurrency = @Concurrency
";
        }
    }
}