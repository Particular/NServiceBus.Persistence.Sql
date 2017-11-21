using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class SubscriptionWriter
{
    public static void WriteSubscriptionScript(string scriptPath, BuildSqlDialect sqlDialect)
    {
        var createPath = Path.Combine(scriptPath, "Subscription_Create.sql");
        File.Delete(createPath);
        using (var writer = File.CreateText(createPath))
        {
            SubscriptionScriptBuilder.BuildCreateScript(writer, sqlDialect);
        }
        var dropPath = Path.Combine(scriptPath, "Subscription_Drop.sql");
        File.Delete(dropPath);
        using (var writer = File.CreateText(dropPath))
        {
            SubscriptionScriptBuilder.BuildCreateScript(writer, sqlDialect);
        }
    }
}