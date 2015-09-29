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
        return string.Format(@"
INSERT INTO [{0}].[{1}.{2}] 
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
)", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(sagaDataType));
    }

    public string BuildUpdateCommand(Type sagaDataType)
    {
        return string.Format(@"
UPDATE [{0}].[{1}.{2}] 
SET
    Originator = @Originator, 
    OriginalMessageId = @OriginalMessageId, 
    Data = @Data, 
    PersistenceVersion = @PersistenceVersion, 
    SagaTypeVersion = @SagaTypeVersion
WHERE
    Id = @Id
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(sagaDataType));
    }

    public string BuildGetBySagaIdCommand(Type sagaDataType) 
    {
        return string.Format(@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data, 
    SagaTypeVersion
FROM  [{0}].[{1}.{2}] 
WHERE Id = @Id
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(sagaDataType));
    }

    public string BuildGetByPropertyCommand(Type sagaDataType, string propertyName) 
    {
        return string.Format(@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data, 
    SagaTypeVersion
FROM  [{0}].[{1}.{2}] 
WHERE [Data].exist('/Data/{3}[.= (sql:variable(""@propertyValue""))]') = 1
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(sagaDataType), propertyName);
    }

    public string BuildCompleteCommand(Type sagaDataType)
    {
        return string.Format(@"
DELETE FROM  [{0}].[{1}.{2}] 
WHERE Id = @Id
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(sagaDataType));
    }
}
