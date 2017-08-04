using System;
using System.IO;
using System.Reflection;

static class ScriptLocation
{
    public static string FindScriptDirectory(Type sqlVariant)
    {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var currentDirectory = Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.Sql", sqlVariant.ToString());
    }

    public static void ValidateScriptExists(string createScript)
    {
        if (!File.Exists(createScript))
        {
            throw new Exception($"Expected '{createScript}' to exist. It is possible it was not deployed with the endpoint or NServiceBus.Persistence.Sql.MsBuild nuget was not included in the project.");
        }
    }
}