using System;
using System.Data;
using System.Data.SqlClient;
using NServiceBus.Saga;

class SagaPersister : ISagaPersister
{
    string connectionString;
    string schema;
    string endpointName;

    public SagaPersister(string connectionString, string schema, string endpointName)
    {
        this.connectionString = connectionString;
        this.schema = schema;
        this.endpointName = endpointName;
    }

    public void Save(IContainSagaData data)
    {
        var type = data.GetType();
        var saveComand = string.Format(@"
INSERT INTO [{0}].[{1}.{2}] 
(
    Id, 
    Originator, 
    OriginalMessageId, 
    Data
) 
VALUES 
(
    @Id, 
    @Originator, 
    @OriginalMessageId, 
    @Data
)", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(type));
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(saveComand, connection))
        {
            command.AddParameter("Id", data.Id);
            command.AddParameter("OriginalMessageId", data.OriginalMessageId);
            command.AddParameter("Originator", data.Originator);
            command.AddParameter("Data", SagaSerializer.ToXml(data));
            command.ExecuteNonQuery();
        }
    }

    public void Update(IContainSagaData data)
    {
        var type = data.GetType();
        var saveComand = string.Format(@"
UPDATE [{0}].[{1}.{2}] 
SET
    Originator = @Originator, 
    OriginalMessageId = @OriginalMessageId, 
    Data = @Data
WHERE
    Id = @Id
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(type));
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(saveComand, connection))
        {
            command.AddParameter("Id", data.Id);
            command.AddParameter("OriginalMessageId", data.OriginalMessageId);
            command.AddParameter("Originator", data.Originator);
            command.Parameters.AddWithValue("Data", SagaSerializer.ToXml(data));
            command.ExecuteNonQuery();
        }
    }

    public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
    {
        var getComand = string.Format(@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data
FROM  [{0}].[{1}.{2}] 
WHERE Id = @Id
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(typeof(TSagaData)));
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(getComand, connection))
        {
            command.AddParameter("Id", sagaId);
            return GetSagaData<TSagaData>(command);
        }
    }

    public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
    {
        var getComand = string.Format(@"
SELECT
    Id,
    Originator,
    OriginalMessageId,
    Data
FROM  [{0}].[{1}.{2}] 
WHERE [Data].exist('/Data/{3}[.= (sql:variable(""@propertyValue""))]') = 1
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(typeof(TSagaData)), propertyName);
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(getComand, connection))
        {
            command.AddParameter("propertyValue", propertyValue);
            return GetSagaData<TSagaData>(command);
        }
    }


    static TSagaData GetSagaData<TSagaData>(SqlCommand command) where TSagaData : IContainSagaData
    {
        using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
            if (!reader.Read())
            {
                return default(TSagaData);
            }
            var id = reader.GetGuid(0);
            var originator = reader.GetString(1);
            var originalMessageId = reader.GetString(2);
            using (var data = reader.GetSqlXml(3).CreateReader())
            {
                var sagaData = SagaSerializer.FromString<TSagaData>(data);
                sagaData.Id = id;
                sagaData.Originator = originator;
                sagaData.OriginalMessageId = originalMessageId;
                return sagaData;
            }
        }
    }

    public void Complete(IContainSagaData data)
    {
        var type = data.GetType();
        var completeComand = string.Format(@"
DELETE FROM  [{0}].[{1}.{2}] 
WHERE Id = @Id
", schema, endpointName, SagaTableNameBuilder.GetTableSuffix(type));
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(completeComand, connection))
        {
            command.AddParameter("Id", data.Id);
            command.ExecuteReader();
        }
    }
}
