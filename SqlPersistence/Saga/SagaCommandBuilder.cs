using System.Text;

class SagaCommandBuilder
{
    string tablePrefix;

    public SagaCommandBuilder(string tablePrefix)
    {
        this.tablePrefix = tablePrefix;
    }

    public string BuildSaveCommand(string tableSuffx, string correlationProperty, string transitionalCorrelationProperty)
    {
        var valuesBuilder = new StringBuilder();
        var insertBuilder = new StringBuilder();

        if (correlationProperty != null)
        {
            insertBuilder.Append($",\r\nCorrelation_{correlationProperty}");
            valuesBuilder.Append(",\r\n@CorrelationId");
        }
        if (transitionalCorrelationProperty != null)
        {
            insertBuilder.Append($",\r\nCorrelation_{transitionalCorrelationProperty}");
            valuesBuilder.Append(",\r\n@TransitionalCorrelationId");
        }

        return $@"
insert into {tablePrefix}{tableSuffx}
(
    Id,
    Metadata,
    Data,
    PersistenceVersion,
    SagaTypeVersion,
    SagaVersion{insertBuilder}
)
VALUES
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
            correlationSet = $",\r\nCorrelation_{transitionalCorrelationProperty} = @TransitionalCorrelationId";
        }

        return $@"
update {tablePrefix}{tableSuffx}
SET
    Data = @Data,
    PersistenceVersion = @PersistenceVersion,
    SagaTypeVersion = @SagaTypeVersion,
    SagaVersion = @SagaVersion + 1{correlationSet}
WHERE
    Id = @Id AND SagaVersion = @SagaVersion
";
    }

    public string BuildGetBySagaIdCommand(string tableSuffx)
    {
        return $@"
select
    Id,
    Metadata,
    Data,
    SagaTypeVersion,
    SagaVersion
from {tablePrefix}{tableSuffx}
where Id = @Id
";
    }

    public string BuildGetByPropertyCommand(string tableSuffx, string propertyName)
    {
        return $@"
select
    Id,
    Metadata,
    Data,
    SagaTypeVersion,
    SagaVersion
from {tablePrefix}{tableSuffx}
where Correlation_{propertyName} = @propertyValue
";
    }

    public string BuildCompleteCommand(string tableSuffx)
    {
        return $@"
delete from {tablePrefix}{tableSuffx}
where Id = @Id AND SagaVersion = @SagaVersion
";
    }
}
