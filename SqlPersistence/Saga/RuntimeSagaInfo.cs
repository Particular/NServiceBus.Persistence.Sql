using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using NewtonSerializer = Newtonsoft.Json.JsonSerializer;

class RuntimeSagaInfo
{
    Type sagaDataType;
    VersionDeserializeBuilder versionDeserializeBuilder;
    NewtonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<StringBuilder, JsonWriter> writerCreator;
    ConcurrentDictionary<Version, NewtonSerializer> deserializers;
    public readonly Version CurrentVersion;
    public readonly string CompleteCommand;
    public readonly string GetBySagaIdCommand;
    public readonly string SaveCommand;
    public readonly string UpdateCommand;
    public readonly Func<IContainSagaData, object> TransitionalAccessor;
    public readonly bool HasTransitionalCorrelationProperty;
    public readonly bool HasCorrelationProperty;
    public readonly string CorrelationProperty;
    public readonly string TransitionalCorrelationProperty;
    public readonly string GetByCorrelationPropertyCommand;

    public RuntimeSagaInfo(
        SagaCommandBuilder commandBuilder,
        Type sagaDataType,
        VersionDeserializeBuilder versionDeserializeBuilder,
        Type sagaType,
        NewtonSerializer jsonSerializer,
            Func<TextReader, JsonReader> readerCreator,
            Func<StringBuilder, JsonWriter> writerCreator)
    {
        this.sagaDataType = sagaDataType;
        if (versionDeserializeBuilder != null)
        {
            deserializers = new ConcurrentDictionary<Version, NewtonSerializer>();
        }
        this.versionDeserializeBuilder = versionDeserializeBuilder;
        this.jsonSerializer = jsonSerializer;
        this.readerCreator = readerCreator;
        this.writerCreator = writerCreator;
        CurrentVersion = sagaDataType.Assembly.GetFileVersion();
        var sqlSagaAttributeData = SqlSagaAttributeReader.GetSqlSagaAttributeData(sagaType);
        var tableSuffix = sqlSagaAttributeData.TableSuffix;
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


    public string ToJson(IContainSagaData sagaData)
    {
        var builder = new StringBuilder();
        using (var writer = writerCreator(builder))
        {
            jsonSerializer.Serialize(writer, sagaData);
        }
        return builder.ToString();
    }


    public TSagaData FromString<TSagaData>(TextReader reader, Version storedSagaTypeVersion)
        where TSagaData : IContainSagaData
    {
        var serializer = GetDeserialize(storedSagaTypeVersion);
        using (var jsonReader = readerCreator(reader))
        {
            return serializer.Deserialize<TSagaData>(jsonReader);
        }
    }


    NewtonSerializer GetDeserialize(Version storedSagaTypeVersion)
    {
        if (versionDeserializeBuilder == null)
        {
            return jsonSerializer;
        }
        return deserializers.GetOrAdd(storedSagaTypeVersion, _ =>
        {
            var settings = versionDeserializeBuilder(sagaDataType, storedSagaTypeVersion);
            if (settings != null)
            {
                return JsonSerializer.Create(settings); 
            }
            return jsonSerializer;
        });
    }

}