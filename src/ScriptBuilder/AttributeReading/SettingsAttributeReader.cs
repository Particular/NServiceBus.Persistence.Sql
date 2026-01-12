#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SettingsAttributeReader
{
    public static Settings Read(string assemblyPath)
    {
        Settings? settings = null;
        List<SagaDefinition> sagas = [];

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var assemblyDef = reader.GetAssemblyDefinition();

        foreach (var handle in assemblyDef.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);

            if (attribute.Constructor.Kind == HandleKind.MemberReference)
            {
                var typeHandle = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
                var typeReference = reader.GetTypeReference((TypeReferenceHandle)typeHandle);
                var attName = reader.GetString(typeReference.Name);

                if (attName == "SqlPersistenceSettingsAttribute")
                {
                    var attNamespace = reader.GetString(typeReference.Namespace);
                    if (attNamespace == "NServiceBus.Persistence.Sql")
                    {
                        var args = attribute.DecodeValue(new AttributeTypeProvider());
                        var properties = args.NamedArguments.ToDictionary(o => o.Name!, o => o.Value);
                        settings = ReadFromProperties(properties);
                    }
                }
            }
            else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
            {
                var typeHandle = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
                var typeReference = reader.GetTypeDefinition(typeHandle);
                var attName = reader.GetString(typeReference.Name);

                if (attName == "NServiceBusGeneratedSqlSagaMetadataAttribute")
                {
                    var attNamespace = reader.GetString(typeReference.Namespace);
                    if (string.IsNullOrEmpty(attNamespace))
                    {
                        var args = attribute.DecodeValue(new AttributeTypeProvider());
                        var properties = args.NamedArguments.ToDictionary(o => o.Name!, o => o.Value);

                        var sagaType = properties.GetValueOrDefault("SagaType") as string;
                        var corrName = properties.GetValueOrDefault("CorrelationPropertyName") as string;
                        var corrType = properties.GetValueOrDefault("CorrelationPropertyType") as string;
                        var transName = properties.GetValueOrDefault("TransitionalCorrelationPropertyName") as string;
                        var transType = properties.GetValueOrDefault("TransitionalCorrelationPropertyType") as string;
                        var tableSuffix = properties.GetValueOrDefault("TableSuffix") as string;

                        var correlation = GetCorrelation(corrName, corrType);
                        var transitionalCorrelation = GetCorrelation(transName, transType);

                        if (tableSuffix is not null && sagaType is not null)
                        {
                            var definition = new SagaDefinition(tableSuffix, sagaType, correlation, transitionalCorrelation);
                            sagas.Add(definition);
                        }
                    }
                }
            }
        }

        // If no attribute, generate the default
        settings ??= ReadFromProperties([]);

        // Then apply the saga definitions
        settings.SagaDefinitions = sagas.ToArray();
        return settings;
    }

    public static Settings ReadFromProperties(Dictionary<string, object?> properties)
    {
        return new Settings
        {
            BuildDialects = ReadBuildDialects(properties).ToList(),
            ScriptPromotionPath = ReadScriptPromotionPath(properties),
            ProduceSagaScripts = GetBoolProperty(properties, "ProduceSagaScripts", true),
            ProduceTimeoutScripts = GetBoolProperty(properties, "ProduceTimeoutScripts", true),
            ProduceSubscriptionScripts = GetBoolProperty(properties, "ProduceSubscriptionScripts", true),
            ProduceOutboxScripts = GetBoolProperty(properties, "ProduceOutboxScripts", true),
        };
    }

    static bool GetBoolProperty(Dictionary<string, object?> properties, string key, bool defaultValue = false)
        => properties.GetValueOrDefault(key) as bool? ?? defaultValue;

    static string? ReadScriptPromotionPath(Dictionary<string, object?> properties)
    {
        if (properties.GetValueOrDefault("ScriptPromotionPath") is not string target)
        {
            return null;
        }
        return !string.IsNullOrWhiteSpace(target) ? target : throw new ErrorsException("SqlPersistenceSettingsAttribute contains an empty ScriptPromotionPath.");
    }

    static IEnumerable<BuildSqlDialect> ReadBuildDialects(Dictionary<string, object?> properties)
    {
        if (properties.Count == 0)
        {
            yield return BuildSqlDialect.MsSqlServer;
            yield return BuildSqlDialect.MySql;
            yield return BuildSqlDialect.PostgreSql;
            yield return BuildSqlDialect.Oracle;
            yield break;
        }

        var msSqlServerScripts = GetBoolProperty(properties, "MsSqlServerScripts");
        if (msSqlServerScripts)
        {
            yield return BuildSqlDialect.MsSqlServer;
        }

        var mySqlScripts = GetBoolProperty(properties, "MySqlScripts");
        if (mySqlScripts)
        {
            yield return BuildSqlDialect.MySql;
        }

        var postgreSqlScripts = GetBoolProperty(properties, "PostgreSqlScripts");
        if (postgreSqlScripts)
        {
            yield return BuildSqlDialect.PostgreSql;
        }

        var oracleScripts = GetBoolProperty(properties, "OracleScripts");
        if (oracleScripts)
        {
            yield return BuildSqlDialect.Oracle;
        }

        if (!msSqlServerScripts && !mySqlScripts && !oracleScripts && !postgreSqlScripts)
        {
            throw new ErrorsException("Must define at least one of MsSqlServerScripts, MySqlScripts, OracleScripts, or PostgreSqlScripts. Add a [SqlPersistenceSettingsAttribute] to the assembly.");
        }
    }

    static CorrelationProperty? GetCorrelation(string? name, string? type)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        if (!Enum.TryParse<CorrelationPropertyType>(type, out var propType))
        {
            throw new Exception($"Invalid correlation property type '{type}' found in metadata attribute.");
        }

        return new CorrelationProperty(name, propType);
    }

    // Minimal implementation that only supports primitive types
    sealed class AttributeTypeProvider : ICustomAttributeTypeProvider<object?>
    {
        public object? GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode;
        public object? GetSystemType() => typeof(Type);
        public object? GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte raw) => null;
        public object? GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte raw) => null;
        public object? GetSZArrayType(object? elementType) => null;
        public object? GetTypeFromSerializedName(string name) => null;
        public PrimitiveTypeCode GetUnderlyingEnumType(object? type) => PrimitiveTypeCode.Int32;
        public bool IsSystemType(object? type) => false;
    }
}