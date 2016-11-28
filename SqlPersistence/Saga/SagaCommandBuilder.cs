using System.Text;

class SagaCommandBuilder
{
    string schema;
    string endpointName;

    public SagaCommandBuilder(string schema, string endpointName)
    {
        this.schema = schema;
        this.endpointName = endpointName;
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
INSERT INTO [{schema}].[{endpointName}{tableSuffx}]
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
UPDATE [{schema}].[{endpointName}{tableSuffx}]
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
        //TODO: no need to return id if we already have it
        return $@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data,
    SagaTypeVersion
FROM  [{schema}].[{endpointName}{tableSuffx}]
WHERE Id = @Id
";
    }

    public string BuildGetByPropertyCommand(string tableSuffx, string propertyName)
    {
        //TODO: throw if property name
        return $@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data,
    SagaTypeVersion
FROM  [{schema}].[{endpointName}{tableSuffx}]
WHERE [Data].exist('/Data/{propertyName}[.= (sql:variable(""@propertyValue""))]') = 1
";
    }

    public string BuildCompleteCommand(string tableSuffx)
    {
        return $@"
DELETE FROM  [{schema}].[{endpointName}{tableSuffx}]
WHERE Id = @Id
";
    }
}
