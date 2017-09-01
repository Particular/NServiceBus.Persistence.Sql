using System;
using System.IO;
using System.Reflection;
using NServiceBus;

static class ScriptLocation
{
    public static string FindScriptDirectory(SqlDialect dialect)
    {
        var codeBase = Assembly.GetEntryAssembly().CodeBase;
        var currentDirectory = Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.Sql", dialect.Name);
    }

    public static void ValidateScriptExists(string createScript)
    {
        if (!File.Exists(createScript))
        {
            throw new Exception($"Expected '{createScript}' to exist. It is possible it was not deployed with the endpoint or NServiceBus.Persistence.Sql.MsBuild nuget was not included in the project.");
        }
    }
}