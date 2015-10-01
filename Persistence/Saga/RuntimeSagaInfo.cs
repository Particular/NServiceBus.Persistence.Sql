using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence.Saga;

class RuntimeSagaInfo
{
    SagaCommandBuilder commandBuilder;
    Type sagaDataType;
    DeserializeBuilder deserializeBuilder;
    ConcurrentDictionary<Version, Deserialize> deserializers;
    Serialize defaultSerialize;
    public readonly Version CurrentVersion;
    public readonly string CompleteCommand;
    public readonly string GetBySagaIdCommand;
    public readonly string SaveCommand;
    public readonly string UpdateCommand;
    Deserialize defaultDeserialize;

    ConcurrentDictionary<string, string> mappedPropertyCommands = new ConcurrentDictionary<string, string>();

    public RuntimeSagaInfo(
        SagaCommandBuilder commandBuilder, 
        Type sagaDataType,
        DeserializeBuilder deserializeBuilder,
        SerializeBuilder serializeBuilder,
        Action<XmlSerializer, Type> xmlSerializerCustomize)
    {
        this.sagaDataType = sagaDataType;
        if (deserializeBuilder != null)
        {
            deserializers = new ConcurrentDictionary<Version, Deserialize>();
        }
        var defualtSerialization = GetSerializer(serializeBuilder, sagaDataType, xmlSerializerCustomize);
        defaultSerialize = defualtSerialization.Serialize;
        defaultDeserialize = defualtSerialization.Deserialize;
        this.commandBuilder = commandBuilder;
        this.deserializeBuilder = deserializeBuilder;
        CurrentVersion = sagaDataType.Assembly.GetFileVersion();
        CompleteCommand= commandBuilder.BuildCompleteCommand(sagaDataType);
        GetBySagaIdCommand = commandBuilder.BuildGetBySagaIdCommand(sagaDataType);
        SaveCommand = commandBuilder.BuildSaveCommand(sagaDataType);
        UpdateCommand = commandBuilder.BuildUpdateCommand(sagaDataType);
    }


    static DefualtSerialization GetSerializer(SerializeBuilder serializeBuilder, Type sagaDataType, Action<XmlSerializer, Type> xmlSerializerCustomize)
    {
        var serialization = serializeBuilder?.Invoke(sagaDataType);
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
        return commandBuilder.BuildGetByPropertyCommand(sagaDataType,propertyName);
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
        return (TSagaData) deserialize(reader);
    }

    Deserialize GetDeserialize(Version storedSagaTypeVersion) 
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