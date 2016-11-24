class SagaCommandBuilder
{
    string schema;
    string endpointName;

    public SagaCommandBuilder(string schema, string endpointName)
    {
        this.schema = schema;
        this.endpointName = endpointName;
    }

    public string BuildSaveCommand(string tableSuffx)
    {
        return $@"
INSERT INTO [{schema}].[{endpointName}{tableSuffx}]
(
    Id,
    Originator,
    OriginalMessageId,
    Data,
    PersistenceVersion,
    SagaTypeVersion
)
VALUES
(
    @Id,
    @Originator,
    @OriginalMessageId,
    @Data,
    @PersistenceVersion,
    @SagaTypeVersion
)";
    }

    public string BuildUpdateCommand(string tableSuffx)
    {
        return $@"
UPDATE [{schema}].[{endpointName}{tableSuffx}]
SET
    Originator = @Originator,
    OriginalMessageId = @OriginalMessageId,
    Data = @Data,
    PersistenceVersion = @PersistenceVersion,
    SagaTypeVersion = @SagaTypeVersion
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
