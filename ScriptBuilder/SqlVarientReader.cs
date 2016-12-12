using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    static class SqlVarientReader
    {
        public static IEnumerable<BuildSqlVarient> Read(ModuleDefinition moduleDefinition)
        {
            var assemblyCustomAttributes = moduleDefinition.Assembly.CustomAttributes;
            var customAttribute = assemblyCustomAttributes
                .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");
            if (customAttribute == null)
            {
                yield return BuildSqlVarient.MsSqlServer;
                yield return BuildSqlVarient.MySql;
                yield break;
            }
            var arguments = customAttribute.ConstructorArguments;
            var msSqlServerScripts = (bool)arguments[0].Value;
            var mySqlScripts = (bool)arguments[1].Value;
            if (msSqlServerScripts)
            {
                yield return BuildSqlVarient.MsSqlServer;
            }
            if (mySqlScripts)
            {
                yield return BuildSqlVarient.MySql;
            }
            if (!msSqlServerScripts && !mySqlScripts)
            {
                throw new Exception("Must define either MsSqlServerScripts or MySqlScripts ");
            }
        }
    }
}