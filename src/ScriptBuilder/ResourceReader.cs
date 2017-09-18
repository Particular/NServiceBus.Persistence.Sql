using System.IO;
using System.Reflection;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class ResourceReader
{
    static Assembly assembly = typeof(ResourceReader).GetTypeInfo().Assembly;

    public static string ReadResource(BuildSqlDialect sqlDialect, string prefix)
    {
        var text = $"NServiceBus.Persistence.Sql.{prefix}_{sqlDialect}.sql";
        using (var stream = assembly.GetManifestResourceStream(text))
        using (var streamReader = new StreamReader(stream))
        {
            return streamReader.ReadToEnd();
        }
    }
}