using System.Linq;
using Mono.Cecil;

static class OutputPathReader
{
    public static string Read(ModuleDefinition moduleDefinition)
    {
        var assemblyCustomAttributes = moduleDefinition.Assembly.CustomAttributes;
        var customAttribute = assemblyCustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");
        if (customAttribute == null)
        {
            return string.Empty;
        }

        var arguments = customAttribute.ConstructorArguments;
        return (string) arguments[2].Value;
    }

}