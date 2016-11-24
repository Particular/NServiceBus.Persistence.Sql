using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using XmlSerializer = System.Xml.Serialization.XmlSerializer;

class RuntimeSagaInfo
{
    SagaCommandBuilder commandBuilder;
    Type sagaDataType;
    DeserializeBuilder deserializeBuilder;
    ConcurrentDictionary<Version, SagaDeserialize> deserializers;
    SagaSerialize defaultSerialize;
    public readonly Version CurrentVersion;
    public readonly string CompleteCommand;
    public readonly string GetBySagaIdCommand;
    public readonly string SaveCommand;
    public readonly string UpdateCommand;
    SagaDeserialize defaultDeserialize;
    string tableSuffix;

    ConcurrentDictionary<string, string> mappedPropertyCommands = new ConcurrentDictionary<string, string>();

    public RuntimeSagaInfo(
        SagaCommandBuilder commandBuilder,
        Type sagaDataType,
        DeserializeBuilder deserializeBuilder,
        SagaSerializeBuilder sagaSerializeBuilder,
        Action<XmlSerializer, Type> xmlSerializerCustomize,
        Type sagaType)
    {
        this.sagaDataType = sagaDataType;
        if (deserializeBuilder != null)
        {
            deserializers = new ConcurrentDictionary<Version, SagaDeserialize>();
        }
        var defaultSerialization = GetSerializer(sagaSerializeBuilder, sagaDataType, xmlSerializerCustomize);
        defaultSerialize = defaultSerialization.Serialize;
        defaultDeserialize = defaultSerialization.Deserialize;
        this.commandBuilder = commandBuilder;
        this.deserializeBuilder = deserializeBuilder;
        CurrentVersion = sagaDataType.Assembly.GetFileVersion();
        tableSuffix = SagaTableNameBuilder.GetTableSuffix(sagaType);
        CompleteCommand= commandBuilder.BuildCompleteCommand(tableSuffix);
        GetBySagaIdCommand = commandBuilder.BuildGetBySagaIdCommand(tableSuffix);
        SaveCommand = commandBuilder.BuildSaveCommand(tableSuffix);
        UpdateCommand = commandBuilder.BuildUpdateCommand(tableSuffix);
    }


    static DefaultSagaSerialization GetSerializer(SagaSerializeBuilder sagaSerializeBuilder, Type sagaDataType, Action<XmlSerializer, Type> xmlSerializerCustomize)
    {
        var serialization = sagaSerializeBuilder?.Invoke(sagaDataType);
        if (serialization != null)
        {
            return serialization;
        }
        return SagaXmlSerializerBuilder.BuildSerializationDelegate(sagaDataType, xmlSerializerCustomize);
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
            defaultSerialize(writer, sagaData);
        }
        return builder.ToString();
    }

    public TSagaData FromString<TSagaData>(XmlReader reader, Version storedSagaTypeVersion) where TSagaData : IContainSagaData
    {
        var deserialize = GetDeserialize(storedSagaTypeVersion);
        return (TSagaData) (IContainSagaData)deserialize(reader);
    }

    SagaDeserialize GetDeserialize(Version storedSagaTypeVersion)
    {
        if (deserializeBuilder == null)
        {
            return defaultDeserialize;
        }
        return deserializers.GetOrAdd(storedSagaTypeVersion, _ =>
        {
            var customDeserialize = deserializeBuilder(sagaDataType,storedSagaTypeVersion);
            if (customDeserialize != null)
            {
                return customDeserialize;
            }
            return defaultDeserialize;
        });
    }

}