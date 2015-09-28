using System;
using System.Data;
using System.Data.SqlClient;
using NServiceBus.Saga;

class SagaPersister : ISagaPersister
{
    string connectionString;
    SagaInfoCache sagaInfoCache;

    public SagaPersister(string connectionString, SagaInfoCache sagaInfoCache)
    {
        this.connectionString = connectionString;
        this.sagaInfoCache = sagaInfoCache;
    }

    public void Save(IContainSagaData data)
    {
        var sagaInfo = sagaInfoCache.GetInfo(data.GetType());
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(sagaInfo.SaveCommand, connection))
        {
            command.AddParameter("Id", data.Id);
            command.AddParameter("OriginalMessageId", data.OriginalMessageId);
            command.AddParameter("Originator", data.Originator);
            command.AddParameter("Data", sagaInfo.ToXml(data));
            command.AddParameter("PersistenceVersion", StaticVersions.PeristenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            command.ExecuteNonQueryEx();
        }
    }

    public void Update(IContainSagaData data)
    {
        var sagaInfo = sagaInfoCache.GetInfo(data.GetType());
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(sagaInfo.UpdateCommand, connection))
        {
            command.AddParameter("Id", data.Id);
            command.AddParameter("OriginalMessageId", data.OriginalMessageId);
            command.AddParameter("Originator", data.Originator);
            command.AddParameter("PersistenceVersion", StaticVersions.PeristenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            command.Parameters.AddWithValue("Data", sagaInfo.ToXml(data));
            command.ExecuteNonQueryEx();
        }
    }

    public TSagaData Get<TSagaData>(Guid sagaId)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof (TSagaData));
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(sagaInfo.GetBySagaIdCommand, connection))
        {
            command.AddParameter("Id", sagaId);
            return GetSagaData<TSagaData>(command, sagaInfo);
        }
    }

    public TSagaData Get<TSagaData>(string propertyName, object propertyValue)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof (TSagaData));
        var commandText = sagaInfo.GetMappedPropertyCommand(propertyName);
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(commandText, connection))
        {
            command.AddParameter("propertyValue", propertyValue.ToString());
            return GetSagaData<TSagaData>(command, sagaInfo);
        }
    }


    static TSagaData GetSagaData<TSagaData>(SqlCommand command, RuntimeSagaInfo sagaInfo)
        where TSagaData : IContainSagaData
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
            var sagaTypeVersion = Version.Parse(reader.GetString(4));
            using (var data = reader.GetSqlXml(3).CreateReader())
            {
                var sagaData = sagaInfo.FromString<TSagaData>(data, sagaTypeVersion);
                sagaData.Id = id;
                sagaData.Originator = originator;
                sagaData.OriginalMessageId = originalMessageId;
                return sagaData;
            }
        }
    }

    public void Complete(IContainSagaData data)
    {
        var sagaInfo = sagaInfoCache.GetInfo(data.GetType());
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(sagaInfo.CompleteCommand, connection))
        {
            command.AddParameter("Id", data.Id);
            command.ExecuteReader();
        }
    }
}