using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SqlVariantReader
{
    public static IEnumerable<BuildSqlVariant> Read(ModuleDefinition moduleDefinition)
    {
        var attribute = moduleDefinition.Assembly.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");
        if (attribute == null)
        {
            yield return BuildSqlVariant.MsSqlServer;
            yield return BuildSqlVariant.MySql;
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

        if (!msSqlServerScripts && !mySqlScripts)
        {
            throw new ErrorsException("Must define either MsSqlServerScripts, MySqlScripts, or both. Add a [SqlPersistenceSettingsAttribute] to the assembly.");
        }
    }
}