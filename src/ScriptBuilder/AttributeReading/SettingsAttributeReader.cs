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
        var attribute = assembly.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");

        return ReadFromAttribute(attribute);
    }

    public static Settings ReadFromAttribute(CustomAttributeData? attribute) => new()
    {
        BuildDialects = [.. ReadBuildDialects(attribute)],
        ScriptPromotionPath = ReadScriptPromotionPath(attribute),
        ProduceSagaScripts = GetAttributeValue(attribute, "ProduceSagaScripts", true),
        ProduceTimeoutScripts = GetAttributeValue(attribute, "ProduceTimeoutScripts", true),
        ProduceSubscriptionScripts = GetAttributeValue(attribute, "ProduceSubscriptionScripts", true),
        ProduceOutboxScripts = GetAttributeValue(attribute, "ProduceOutboxScripts", true),
    };

    static T? GetAttributeValue<T>(CustomAttributeData? attribute, string namedAttributeKey, T? defaultValue)
    {
        if (attribute is null)
        {
            return defaultValue;
        }

        var arg = attribute.NamedArguments.FirstOrDefault(na => na.MemberName == namedAttributeKey);
        if (arg == default)
        {
            return defaultValue;
        }

        return (T?)arg.TypedValue.Value!;
    }

    static string? ReadScriptPromotionPath(CustomAttributeData? attribute)
    {
        var target = GetAttributeValue<string>(attribute, "ScriptPromotionPath", null);
        if (target == null)
        {
            return null;
        }
        if (!string.IsNullOrWhiteSpace(target))
        {
            return target;
        }
        throw new ErrorsException("SqlPersistenceSettingsAttribute contains an empty ScriptPromotionPath.");
    }

    static IEnumerable<BuildSqlDialect> ReadBuildDialects(CustomAttributeData? attribute)
    {
        if (attribute == null)
        {
            yield return BuildSqlDialect.MsSqlServer;
            yield return BuildSqlDialect.MySql;
            yield return BuildSqlDialect.PostgreSql;
            yield return BuildSqlDialect.Oracle;
            yield break;
        }

        var msSqlServerScripts = GetAttributeValue(attribute, "MsSqlServerScripts", false);
        if (msSqlServerScripts)
        {
            yield return BuildSqlDialect.MsSqlServer;
        }

        var mySqlScripts = GetAttributeValue(attribute, "MySqlScripts", false);
        if (mySqlScripts)
        {
            yield return BuildSqlDialect.MySql;
        }

        var postgreSqlScripts = GetAttributeValue(attribute, "PostgreSqlScripts", false);
        if (postgreSqlScripts)
        {
            yield return BuildSqlDialect.PostgreSql;
        }

        var oracleScripts = GetAttributeValue(attribute, "OracleScripts", false);
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