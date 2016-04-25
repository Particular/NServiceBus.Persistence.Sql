using System;
using System.IO;
using System.Reflection;

static class ScriptLocation
{
    public static string FindScriptDirectory()
    {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var currentDirectory = Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.SqlServerXml");
    }
}