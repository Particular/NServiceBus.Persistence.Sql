using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using NServiceBus;
using NServiceBus.Persistence.Sql;

class RuntimeSagaInfo<TReader> 
    where TReader : IDisposable
{
    Type sagaDataType;
    SagaDeserialize<TReader> deserialize;
    SagaSerialize serialize;
    SqlPersistenceSerializer<TReader> sqlPersistenceSerializer;
    ConcurrentDictionary<Version, SagaDeserialize<TReader>> deserializers;
    public readonly Version CurrentVersion;
    public readonly string CompleteCommand;
    public readonly string GetBySagaIdCommand;
    public readonly string SaveCommand;
    public readonly string UpdateCommand;
    public readonly Func<IContainSagaData, object> TransitionalAccessor;
    public readonly bool HasTransitionalCorrelationProperty;
    string tableSuffix;
    public readonly bool HasCorrelationProperty;
    public readonly string CorrelationProperty;
    public readonly string TransitionalCorrelationProperty;
    public readonly string GetByCorrelationPropertyCommand;

    public RuntimeSagaInfo(
        SagaCommandBuilder commandBuilder,
        Type sagaDataType,
        Type sagaType, 
        SqlPersistenceSerializer<TReader> sqlPersistenceSerializer)
    {
        this.sagaDataType = sagaDataType;
        if (sqlPersistenceSerializer.VersionDeserializeBuilder != null)
        {
            deserializers = new ConcurrentDictionary<Version, SagaDeserialize<TReader>>();
        }
        var defaultSagaSerialization = sqlPersistenceSerializer.SerializationBuilder(sagaDataType);
        deserialize = defaultSagaSerialization.Deserialize;
        serialize = defaultSagaSerialization.Serialize;
        this.sqlPersistenceSerializer = sqlPersistenceSerializer;
        CurrentVersion = sagaDataType.Assembly.GetFileVersion();
        var sqlSagaAttributeData = SqlSagaAttributeReader.GetSqlSagaAttributeData(sagaType);
        tableSuffix = sqlSagaAttributeData.TableSuffix;
        CompleteCommand = commandBuilder.BuildCompleteCommand(tableSuffix);
        GetBySagaIdCommand = commandBuilder.BuildGetBySagaIdCommand(tableSuffix);
        SaveCommand = commandBuilder.BuildSaveCommand(tableSuffix, sqlSagaAttributeData.CorrelationProperty, sqlSagaAttributeData.TransitionalCorrelationProperty);
        UpdateCommand = commandBuilder.BuildUpdateCommand(tableSuffix, sqlSagaAttributeData.TransitionalCorrelationProperty);

        CorrelationProperty = sqlSagaAttributeData.CorrelationProperty;
        HasCorrelationProperty = CorrelationProperty != null;
        if (HasCorrelationProperty)
        {
            GetByCorrelationPropertyCommand = commandBuilder.BuildGetByPropertyCommand(tableSuffix, sqlSagaAttributeData.CorrelationProperty);
        }

        TransitionalCorrelationProperty = sqlSagaAttributeData.TransitionalCorrelationProperty;
        HasTransitionalCorrelationProperty = TransitionalCorrelationProperty != null;
        if (HasTransitionalCorrelationProperty)
        {
            TransitionalAccessor = sagaDataType.GetPropertyAccessor<IContainSagaData>(TransitionalCorrelationProperty);
        }
    }


    public string SagaToString(IContainSagaData sagaData)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            serialize(writer, sagaData);
        }
        return builder.ToString();
    }


    public TSagaData SagaFromReader<TSagaData>(TReader reader, Version storedSagaTypeVersion) where TSagaData : IContainSagaData
    {
        var deserialize = GetDeserialize(storedSagaTypeVersion);
        return (TSagaData) deserialize(reader);
    }


    SagaDeserialize<TReader> GetDeserialize(Version storedSagaTypeVersion)
    {
        var deserializeBuilder = sqlPersistenceSerializer.VersionDeserializeBuilder;
        if (deserializeBuilder == null)
        {
            return deserialize;
        }
        return deserializers.GetOrAdd(storedSagaTypeVersion, _ =>
        {
            var customDeserialize = deserializeBuilder(sagaDataType,storedSagaTypeVersion);
            if (customDeserialize != null)
            {
                return customDeserialize;
            }
            return deserialize;
        });
    }

}