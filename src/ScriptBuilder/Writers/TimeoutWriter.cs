using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class TimeoutWriter
{
    public static void WriteTimeoutScript(string scriptPath, BuildSqlDialect sqlDialect)
    {
        var createPath = Path.Combine(scriptPath, "Timeout_Create.sql");
        File.Delete(createPath);
        using (var writer = File.CreateText(createPath))
        {
            TimeoutScriptBuilder.BuildCreateScript(writer, sqlDialect);
        }
        var dropPath = Path.Combine(scriptPath, "Timeout_Drop.sql");
        File.Delete(dropPath);
        using (var writer = File.CreateText(dropPath))
        {
            TimeoutScriptBuilder.BuildDropScript(writer, sqlDialect);
        }
    }
}