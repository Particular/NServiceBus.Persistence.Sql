﻿using System;
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
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<TextWriter, JsonWriter> writerCreator;
    ConcurrentDictionary<Version, JsonSerializer> deserializers;
    CommandBuilder commandBuilder;
    public readonly Version CurrentVersion;
    public readonly string CompleteCommand;
    public readonly string SelectFromCommand;
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
    public readonly Action<DbParameter, string, object> FillParameter;

    public RuntimeSagaInfo(
        SagaCommandBuilder commandBuilder,
        Type sagaDataType,
        RetrieveVersionSpecificJsonSettings versionSpecificSettings,
        Type sagaType,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<TextWriter, JsonWriter> writerCreator,
        string tablePrefix,
        string schema,
        SqlVariant sqlVariant)
    {
        this.sagaDataType = sagaDataType;
        if (versionSpecificSettings != null)
        {
            deserializers = new ConcurrentDictionary<Version, JsonSerializer>();
        }
        this.versionSpecificSettings = versionSpecificSettings;
        this.jsonSerializer = jsonSerializer;
        this.readerCreator = readerCreator;
        this.writerCreator = writerCreator;
        this.commandBuilder = new CommandBuilder(sqlVariant);
        CurrentVersion = sagaDataType.Assembly.GetFileVersion();
        ValidateIsSqlSaga(sagaType);
        var sqlSagaAttributeData = SqlSagaAttributeReader.GetSqlSagaAttributeData(sagaType);
        var tableSuffix = sqlSagaAttributeData.TableSuffix;

        switch (sqlVariant)
        {
            case SqlVariant.MsSqlServer:
                TableName = $"[{schema}].[{tablePrefix}{tableSuffix}]";
                FillParameter = ParameterFiller.Fill;
                break;
            case SqlVariant.MySql:
                TableName = $"`{tablePrefix}{tableSuffix}`";
                FillParameter = ParameterFiller.Fill;
                break;
            case SqlVariant.Oracle:
                TableName = tableSuffix.ToUpper();
                FillParameter = ParameterFiller.OracleFill;
                break;
            default:
                throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
        }

        CompleteCommand = commandBuilder.BuildCompleteCommand(TableName);
        SelectFromCommand = commandBuilder.BuildSelectFromCommand(TableName);
        GetBySagaIdCommand = commandBuilder.BuildGetBySagaIdCommand(TableName);
        SaveCommand = commandBuilder.BuildSaveCommand(sqlSagaAttributeData.CorrelationProperty, sqlSagaAttributeData.TransitionalCorrelationProperty, TableName);
        UpdateCommand = commandBuilder.BuildUpdateCommand(sqlSagaAttributeData.TransitionalCorrelationProperty, TableName);

        CorrelationProperty = sqlSagaAttributeData.CorrelationProperty;
        HasCorrelationProperty = CorrelationProperty != null;
        if (HasCorrelationProperty)
        {
            GetByCorrelationPropertyCommand = commandBuilder.BuildGetByPropertyCommand(sqlSagaAttributeData.CorrelationProperty, TableName);
        }

        TransitionalCorrelationProperty = sqlSagaAttributeData.TransitionalCorrelationProperty;
        HasTransitionalCorrelationProperty = TransitionalCorrelationProperty != null;
        if (HasTransitionalCorrelationProperty)
        {
            TransitionalAccessor = sagaDataType.GetPropertyAccessor<IContainSagaData>(TransitionalCorrelationProperty);
        }
    }

    void ValidateIsSqlSaga(Type sagaType)
    {
        if (!sagaType.IsSubclassOfRawGeneric(typeof(SqlSaga<>)))
        {
            throw new Exception($"Type '{sagaType.FullName}' does not inherit from SqlSaga<T>. Change the type to inherit from SqlSaga<T>.");
        }
    }

    public CommandWrapper CreateCommand(DbConnection connection)
    {
        return commandBuilder.CreateCommand(connection);
    }

    public string ToJson(IContainSagaData sagaData)
    {
        var originalMessageId = sagaData.OriginalMessageId;
        var originator = sagaData.Originator;
        var id = sagaData.Id;
        sagaData.OriginalMessageId = null;
        sagaData.Originator = null;
        sagaData.Id = Guid.Empty;
        try
        {
            var builder = new StringBuilder();
            using (var stringWriter = new StringWriter(builder))
            using (var writer = writerCreator(stringWriter))
            {
                jsonSerializer.Serialize(writer, sagaData);
            }
            return builder.ToString();
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
            return serializer.Deserialize<TSagaData>(jsonReader);
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
            if (settings != null)
            {
                return JsonSerializer.Create(settings);
            }
            return jsonSerializer;
        });
    }

}