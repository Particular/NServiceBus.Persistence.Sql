using System;
using System.IO;
using NServiceBus;
using NServiceBus.Settings;

static class ScriptLocation
{
    const string ScriptFolder = "NServiceBus.Persistence.Sql";

    public static string FindScriptDirectory(ReadOnlySettings settings)
    {
        var currentDirectory = GetScriptsRootPath(settings);
        return Path.Combine(currentDirectory, ScriptFolder, settings.GetSqlDialect().Name);
    }

    static string GetScriptsRootPath(ReadOnlySettings settings)
    {
        if (settings.TryGet("SqlPersistence.ScriptDirectory", out string scriptDirectory))
        {
            return scriptDirectory;
        }

        //NOTE: This is the same logic that Core uses for finding assembly scanning path.
        //      RelativeSearchPath is set for ASP .NET and points to the binaries folder i.e. /bin
        return AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    public static void ValidateScriptExists(string createScript)
    {
        if (!File.Exists(createScript))
        {
            throw new Exception($"Expected '{createScript}' to exist. It is possible it was not deployed with the endpoint.");
        }
    }
}
