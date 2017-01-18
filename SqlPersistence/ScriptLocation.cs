using System;
using System.IO;
using System.Reflection;
using NServiceBus.Persistence.Sql;

static class ScriptLocation
{
    public static string FindScriptDirectory(SqlVariant sqlVariant)
    {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var currentDirectory = Directory.GetParent(new Uri(codeBase).LocalPath).FullName;
        return Path.Combine(currentDirectory, "NServiceBus.Persistence.Sql", sqlVariant.ToString());
    }
}