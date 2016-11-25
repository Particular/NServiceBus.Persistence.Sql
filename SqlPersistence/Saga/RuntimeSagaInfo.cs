using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using NServiceBus;
using NServiceBus.Persistence.Sql;

class RuntimeSagaInfo
{
    SagaCommandBuilder commandBuilder;
    Type sagaDataType;
    VersionDeserializeBuilder versionDeserializeBuilder;
    SagaDeserialize deserialize;
    SagaSerialize serialize;
    ConcurrentDictionary<Version, SagaDeserialize> deserializers;
    public readonly Version CurrentVersion;
    public readonly string CompleteCommand;
    public readonly string GetBySagaIdCommand;
    public readonly string SaveCommand;
    public readonly string UpdateCommand;
    string tableSuffix;

    ConcurrentDictionary<string, string> mappedPropertyCommands = new ConcurrentDictionary<string, string>();

    public RuntimeSagaInfo(
        SagaCommandBuilder commandBuilder,
        Type sagaDataType,
        VersionDeserializeBuilder versionDeserializeBuilder,
        Type sagaType,
        SagaDeserialize deserialize,
        SagaSerialize serialize)
    {
        this.sagaDataType = sagaDataType;
        if (versionDeserializeBuilder != null)
        {
            deserializers = new ConcurrentDictionary<Version, SagaDeserialize>();
        }
        this.commandBuilder = commandBuilder;
        this.versionDeserializeBuilder = versionDeserializeBuilder;
        this.deserialize = deserialize;
        this.serialize = serialize;
        CurrentVersion = sagaDataType.Assembly.GetFileVersion();
        var sqlSagaAttributeData = SqlSagaAttributeReader.GetSqlSagaAttributeData(sagaType);
        tableSuffix = sqlSagaAttributeData.TableSuffix;
        CompleteCommand= commandBuilder.BuildCompleteCommand(tableSuffix);
        GetBySagaIdCommand = commandBuilder.BuildGetBySagaIdCommand(tableSuffix);
        SaveCommand = commandBuilder.BuildSaveCommand(tableSuffix);
        UpdateCommand = commandBuilder.BuildUpdateCommand(tableSuffix);
    }


    public string GetMappedPropertyCommand(string propertyName)
    {
        return mappedPropertyCommands.GetOrAdd(propertyName, _ => BuildGetByPropertyCommand(propertyName));
    }

    string BuildGetByPropertyCommand(string propertyName)
    {
        return commandBuilder.BuildGetByPropertyCommand(tableSuffix, propertyName);
    }

    public string ToXml(IContainSagaData sagaData)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            serialize(writer, sagaData);
        }
        return builder.ToString();
    }

    public TSagaData FromString<TSagaData>(XmlReader reader, Version storedSagaTypeVersion) where TSagaData : IContainSagaData
    {
        var deserialize = GetDeserialize(storedSagaTypeVersion);
        return (TSagaData) deserialize(reader);
    }

    SagaDeserialize GetDeserialize(Version storedSagaTypeVersion)
    {
        if (versionDeserializeBuilder == null)
        {
            return deserialize;
        }
        return deserializers.GetOrAdd(storedSagaTypeVersion, _ =>
        {
            var customDeserialize = versionDeserializeBuilder(sagaDataType,storedSagaTypeVersion);
            if (customDeserialize != null)
            {
                return customDeserialize;
            }
            return deserialize;
        });
    }

}