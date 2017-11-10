using System;
using System.IO;
using System.Reflection;
using NServiceBus;
using NServiceBus.Settings;

static class ScriptLocation
{
    public static string FindScriptDirectory(ReadOnlySettings settings)
    {
        if (settings.TryGet("SqlPersistence.ScriptDirectory", out string scriptDirectory))
        {
            return scriptDirectory;
        }
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var currentDirectory = Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.Sql", settings.GetSqlVariant().ToString());
    }

    public static void ValidateScriptExists(string createScript)
    {
        if (!File.Exists(createScript))
        {
            throw new Exception($"Expected '{createScript}' to exist. It is possible it was not deployed with the endpoint or NServiceBus.Persistence.Sql.MsBuild nuget was not included in the project.");
        }
    }
}