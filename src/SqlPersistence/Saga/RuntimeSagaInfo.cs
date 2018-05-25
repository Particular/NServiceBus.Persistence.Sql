using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
#pragma warning disable 618

class RuntimeSagaInfo
{
    Type sagaDataType;
    RetrieveVersionSpecificJsonSettings versionSpecificSettings;
    public Type SagaType;
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<TextWriter, JsonWriter> writerCreator;
    SqlDialect sqlDialect;
    ConcurrentDictionary<Version, JsonSerializer> deserializers;
    public readonly Version CurrentVersion;
    public readonly string CompleteCommand;
    public readonly Func<string, string> SelectFromCommandBuilder;
    public readonly string GetBySagaIdCommand;
    public readonly string SaveCommand;
    public readonly string UpdateCommand;
    public readonly Func<IContainSagaData, object> TransitionalAccessor;
    public readonly bool HasCorrelationProperty;
    public readonly bool HasTransitionalCorrelationProperty;
    public readonly string CorrelationProperty;
    public readonly string TransitionalCorrelationProperty;
    public readonly string GetByCorrelationPropertyCommand;
    public readonly string TableName;

    public RuntimeSagaInfo(
        Type sagaDataType,
        RetrieveVersionSpecificJsonSettings versionSpecificSettings,
        Type sagaType,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<TextWriter, JsonWriter> writerCreator,
        string tablePrefix,
        SqlDialect sqlDialect,
        Func<string, string> nameFilter)
    {
        this.sagaDataType = sagaDataType;
        if (versionSpecificSettings != null)
        {
            deserializers = new ConcurrentDictionary<Version, JsonSerializer>();
        }
        this.versionSpecificSettings = versionSpecificSettings;
        SagaType = sagaType;
        this.jsonSerializer = jsonSerializer;
        this.readerCreator = readerCreator;
        this.writerCreator = writerCreator;
        this.sqlDialect = sqlDialect;
        CurrentVersion = sagaDataType.Assembly.GetFileVersion();
        ValidateIsSqlSaga();
        var sqlSagaAttributeData = SqlSagaTypeDataReader.GetTypeData(sagaType);
        var tableSuffix = nameFilter(sqlSagaAttributeData.TableSuffix);

        TableName = sqlDialect.GetSagaTableName(tablePrefix, tableSuffix);

        CompleteCommand = sqlDialect.BuildCompleteCommand(TableName);
        SelectFromCommandBuilder = sqlDialect.BuildSelectFromCommand(TableName);
        GetBySagaIdCommand = sqlDialect.BuildGetBySagaIdCommand(TableName);
        SaveCommand = sqlDialect.BuildSaveCommand(sqlSagaAttributeData.CorrelationProperty, sqlSagaAttributeData.TransitionalCorrelationProperty, TableName);
        UpdateCommand = sqlDialect.BuildUpdateCommand(sqlSagaAttributeData.TransitionalCorrelationProperty, TableName);

        CorrelationProperty = sqlSagaAttributeData.CorrelationProperty;
        HasCorrelationProperty = CorrelationProperty != null;
        if (HasCorrelationProperty)
        {
            GetByCorrelationPropertyCommand = sqlDialect.BuildGetByPropertyCommand(sqlSagaAttributeData.CorrelationProperty, TableName);
        }

        TransitionalCorrelationProperty = sqlSagaAttributeData.TransitionalCorrelationProperty;
        HasTransitionalCorrelationProperty = TransitionalCorrelationProperty != null;
        if (HasTransitionalCorrelationProperty)
        {
            TransitionalAccessor = sagaDataType.GetPropertyAccessor<IContainSagaData>(TransitionalCorrelationProperty);
        }
    }

    void ValidateIsSqlSaga()
    {
        if (!SagaType.IsSubclassOfRawGeneric(typeof(SqlSaga<>)))
        {
            throw new Exception($"Type '{SagaType.FullName}' does not inherit from SqlSaga<T>. Change the type to inherit from SqlSaga<T>.");
        }
    }

    public CommandWrapper CreateCommand(DbConnection connection)
    {
        return sqlDialect.CreateCommand(connection);
    }

    public string ToJson(IContainSagaData sagaData)
    {
        var builder = new StringBuilder();
        using (var stringWriter = new StringWriter(builder))
        {
            ToJson(sagaData, stringWriter);
            stringWriter.Flush();
        }

        return builder.ToString();
    }

    public void ToJson(IContainSagaData sagaData, TextWriter textWriter)
    {
        var originalMessageId = sagaData.OriginalMessageId;
        var originator = sagaData.Originator;
        var id = sagaData.Id;
        sagaData.OriginalMessageId = null;
        sagaData.Originator = null;
        sagaData.Id = Guid.Empty;
        try
        {
            using (var writer = writerCreator(textWriter))
            {
                try
                {
                    jsonSerializer.Serialize(writer, sagaData);
                }
                catch (Exception exception)
                {
                    throw new SerializationException(exception);
                }

                writer.Flush();
            }
        }
        finally
        {
            sagaData.OriginalMessageId = originalMessageId;
            sagaData.Originator = originator;
            sagaData.Id = id;
        }
    }

    public TSagaData FromString<TSagaData>(TextReader textReader, Version storedSagaTypeVersion)
        where TSagaData : IContainSagaData
    {
        var serializer = GetDeserialize(storedSagaTypeVersion);
        using (var jsonReader = readerCreator(textReader))
        {
            try
            {
                return serializer.Deserialize<TSagaData>(jsonReader);
            }
            catch (Exception exception)
            {
                throw new SerializationException(exception);
            }
        }
    }


    JsonSerializer GetDeserialize(Version storedSagaTypeVersion)
    {
        if (versionSpecificSettings == null)
        {
            return jsonSerializer;
        }
        return deserializers.GetOrAdd(storedSagaTypeVersion, _ =>
        {
            var settings = versionSpecificSettings(sagaDataType, storedSagaTypeVersion);
            if (settings == null)
            {
                return jsonSerializer;
            }
            var serializer = JsonSerializer.Create(settings);
            sqlDialect.ValidateJsonSettings(serializer);
            return serializer;
        });
    }
}