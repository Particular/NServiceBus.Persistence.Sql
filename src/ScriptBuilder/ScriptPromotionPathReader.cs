using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;

static class ScriptPromotionPathReader
{
    public static bool TryRead(ModuleDefinition moduleDefinition, out string target)
    {
        var customAttribute = moduleDefinition.Assembly.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");
        if (customAttribute == null)
        {
            target = null;
            return false;
        }
        
        target = customAttribute.GetStringField("ScriptPromotionPath");
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