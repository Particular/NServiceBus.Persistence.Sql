using System.Text;

class SagaCommandBuilder
{
    string schema;
    string tablePrefix;

    public SagaCommandBuilder(string schema, string tablePrefix)
    {
        this.schema = schema;
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
insert into {GetTableName(tableSuffx)}
(
    Id,
    Originator,
    OriginalMessageId,
    Data,
    PersistenceVersion,
    SagaTypeVersion{insertBuilder}
)
VALUES
(
    @Id,
    @Originator,
    @OriginalMessageId,
    @Data,
    @PersistenceVersion,
    @SagaTypeVersion{valuesBuilder}
)";
    }


    public string BuildUpdateCommand(string tableSuffx, string transitionalCorrelationProperty)
    {
        // no need to set CorrelationProperty since it is immutable

        var correlationSet = "";
        if (transitionalCorrelationProperty != null)
        {
            correlationSet = $",\r\nCorrelation_{transitionalCorrelationProperty}";
        }

        return $@"
update {GetTableName(tableSuffx)}
SET
    Originator = @Originator,
    OriginalMessageId = @OriginalMessageId,
    Data = @Data,
    PersistenceVersion = @PersistenceVersion,
    SagaTypeVersion = @SagaTypeVersion{correlationSet}
WHERE
    Id = @Id
";
    }

    public string BuildGetBySagaIdCommand(string tableSuffx)
    {
        return $@"
select
    Id,
    Originator,
    OriginalMessageId,
    Data,
    SagaTypeVersion
from {GetTableName(tableSuffx)}
where Id = @Id
";
    }

    public string BuildGetByPropertyCommand(string tableSuffx, string propertyName)
    {
        return $@"
select
    Id,
    Originator,
    OriginalMessageId,
    Data,
    SagaTypeVersion
from {GetTableName(tableSuffx)}
where Correlation_{propertyName} = @propertyValue
";
    }

    public string BuildCompleteCommand(string tableSuffx)
    {
        return $@"
delete from {GetTableName(tableSuffx)}
where Id = @Id
";
    }

    string GetTableName(string tableSuffx)
    {
        if (schema == null)
        {
            return $@"{tablePrefix}{tableSuffx}";
        }
        return $@"{schema}.{tablePrefix}{tableSuffx}";
    }
}
