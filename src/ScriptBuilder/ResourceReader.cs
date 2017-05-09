using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class ResourceReader
{
    public static string ReadResource(BuildSqlVariant sqlVariant, string prefix)
    {
        var text = $"NServiceBus.Persistence.Sql.{prefix}_{sqlVariant}.sql";
        using (var stream = typeof(ResourceReader).Assembly.GetManifestResourceStream(text))
        using (var streamReader = new StreamReader(stream))
        {
            return streamReader.ReadToEnd();
        }
    }

}