using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;

static class ScriptPromotionPathReader
{
    public static bool TryRead(ModuleDefinition moduleDefinition, out string target)
    {
        var assemblyCustomAttributes = moduleDefinition.Assembly.CustomAttributes;
        var customAttribute = assemblyCustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");
        if (customAttribute == null)
        {
            target = null;
            return false;
        }

        var arguments = customAttribute.ConstructorArguments;
        target = (string) arguments[2].Value;
        if (target == null)
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(target))
        {
            return true;
        }
        throw new ErrorsException("SqlPersistenceSettingsAttribute contains an empty ScriptPromotionPath.");
    }

}