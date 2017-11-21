using System;
using System.IO;
using System.Reflection;
using NServiceBus;
using NServiceBus.Settings;

static class ScriptLocation
{
    public static string FindScriptDirectory(ReadOnlySettings settings)
    {
        var currentDirectory = GetCurrentDirectory(settings);
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.Sql", settings.GetSqlDialect().Name);
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
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        var codeBase = entryAssembly.CodeBase;
        return Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
    }

    public static void ValidateScriptExists(string createScript)
    {
        if (!File.Exists(createScript))
        {
            throw new Exception($"Expected '{createScript}' to exist. It is possible it was not deployed with the endpoint or NServiceBus.Persistence.Sql.MsBuild nuget was not included in the project.");
        }
    }
}