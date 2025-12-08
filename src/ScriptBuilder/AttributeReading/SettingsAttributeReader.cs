#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SettingsAttributeReader
{
    public static Settings Read(Assembly assembly)
    {
        var properties = assembly.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute")
            ?.NamedArguments
            .ToDictionary(arg => arg.MemberName, arg => arg.TypedValue.Value) ?? [];

        return ReadFromProperties(properties);
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
}