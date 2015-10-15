using System;
using System.IO;

static class ScriptLocation
{
    public static string FindScriptDirectory()
    {
        var currentDirectory = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.Sql");
    }
}