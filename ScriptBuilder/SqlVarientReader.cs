using System.Linq;
using Mono.Cecil;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    static class SqlVarientReader
    {
        public static SqlVarient Read(ModuleDefinition moduleDefinition)
        {
            var customAttribute = moduleDefinition.Assembly.CustomAttributes
                .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");
            if (customAttribute == null)
            {
                return SqlVarient.All;
            }
            return (SqlVarient)customAttribute.ConstructorArguments.First().Value;
        }
    }
}