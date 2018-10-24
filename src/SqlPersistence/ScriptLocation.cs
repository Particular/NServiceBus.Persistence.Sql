using System;
using System.IO;
using System.Reflection;
using NServiceBus;
using NServiceBus.Settings;

static class ScriptLocation
{
    const string ScriptFolder = "NServiceBus.Persistence.Sql";

    public static string FindScriptDirectory(ReadOnlySettings settings)
    {
        var currentDirectory = GetCurrentDirectory(settings);
        return Path.Combine(currentDirectory, ScriptFolder, settings.GetSqlDialect().Name);
    }

    static string GetCurrentDirectory(ReadOnlySettings settings)
    {
        if (settings.TryGet("SqlPersistence.ScriptDirectory", out string scriptDirectory))
        {
            return scriptDirectory;
        }
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptDir = Path.Combine(baseDir, ScriptFolder);
            //if the app domain base dir contains the scripts folder, return it. Otherwise add "bin" to the base dir so that web apps work correctly
            if (Directory.Exists(scriptDir))
            {
                return baseDir;
            }
            return Path.Combine(baseDir, "bin");
        }
        var codeBase = entryAssembly.CodeBase;
        return Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
    }

    public static void ValidateScriptExists(string createScript)
    {
        if (!File.Exists(createScript))
        {
            throw new Exception($"Expected '{createScript}' to exist. It is possible it was not deployed with the endpoint.");
        }
    }
}
