using System;

class SagaCommandBuilder
{
    string schema;
    string endpointName;

    public SagaCommandBuilder(string schema, string endpointName)
    {
        this.schema = schema;
        this.endpointName = endpointName;
    }

    public string BuildSaveCommand(Type sagaDataType)
    {
        return $@"
INSERT INTO [{schema}].[{endpointName}.{SagaTableNameBuilder.GetTableSuffix(sagaDataType)}] 
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

    public string BuildUpdateCommand(Type sagaDataType)
    {
        return $@"
UPDATE [{schema}].[{endpointName}.{SagaTableNameBuilder.GetTableSuffix(sagaDataType)}] 
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

    public string BuildGetBySagaIdCommand(Type sagaDataType) 
    {
        return $@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data, 
    SagaTypeVersion
FROM  [{schema}].[{endpointName}.{SagaTableNameBuilder.GetTableSuffix(sagaDataType)}] 
WHERE Id = @Id
";
    }

    public string BuildGetByPropertyCommand(Type sagaDataType, string propertyName) 
    {
        return $@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data, 
    SagaTypeVersion
FROM  [{schema}].[{endpointName}.{SagaTableNameBuilder.GetTableSuffix(sagaDataType)}] 
WHERE [Data].exist('/Data/{propertyName}[.= (sql:variable(""@propertyValue""))]') = 1
";
    }

    public string BuildCompleteCommand(Type sagaDataType)
    {
        return $@"
DELETE FROM  [{schema}].[{endpointName}.{SagaTableNameBuilder.GetTableSuffix(sagaDataType)}] 
WHERE Id = @Id
";
    }
}
