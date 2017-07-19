using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SettingsAttributeReader
{
    public static Settings Read(ModuleDefinition module)
    {
        var attribute = module.Assembly.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");

        return ReadFromAttribute(attribute);
    }

    public static Settings ReadFromAttribute(ICustomAttribute attribute)
    {
        return new Settings
        {
            BuildVariants = ReadBuildVariants(attribute).ToList(),
            ScriptPromotionPath = ReadScriptPromotionPath(attribute),
            ProduceSagaScripts = attribute.GetBoolProperty("ProduceSagaScripts", true),
            ProduceTimeoutScripts = attribute.GetBoolProperty("ProduceTimeoutScripts", true),
            ProduceSubscriptionScripts = attribute.GetBoolProperty("ProduceSubscriptionScripts", true),
            ProduceOutboxScripts = attribute.GetBoolProperty("ProduceOutboxScripts", true),
        };
    }

    static string ReadScriptPromotionPath(ICustomAttribute attribute)
    {
        var target = attribute?.GetStringProperty("ScriptPromotionPath");
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

    static IEnumerable<BuildSqlVariant> ReadBuildVariants(ICustomAttribute attribute)
    {
        if (attribute == null)
        {
            yield return BuildSqlVariant.MsSqlServer;
            yield return BuildSqlVariant.MySql;
            yield return BuildSqlVariant.Oracle;
            yield break;
        }

        var msSqlServerScripts = attribute.GetBoolProperty("MsSqlServerScripts");
        if (msSqlServerScripts)
        {
            yield return BuildSqlVariant.MsSqlServer;
        }

        var mySqlScripts = attribute.GetBoolProperty("MySqlScripts");
        if (mySqlScripts)
        {
            yield return BuildSqlVariant.MySql;
        }

        var oracleScripts = attribute.GetBoolProperty("OracleScripts");
        if (oracleScripts)
        {
            yield return BuildSqlVariant.Oracle;
        }

        if (!msSqlServerScripts && !mySqlScripts && !oracleScripts)
        {
            throw new ErrorsException("Must define at least one of MsSqlServerScripts, MySqlScripts, or OracleScripts. Add a [SqlPersistenceSettingsAttribute] to the assembly.");
        }
    }

}