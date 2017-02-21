using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class SubscriptionWriter
{
    public static void WriteSubscriptionScript(string scriptPath, BuildSqlVariant sqlVariant)
    {
        var createPath = Path.Combine(scriptPath, "Subscription_Create.sql");
        File.Delete(createPath);
        using (var writer = File.CreateText(createPath))
        {
            SubscriptionScriptBuilder.BuildCreateScript(writer, sqlVariant);
        }
        var dropPath = Path.Combine(scriptPath, "Subscription_Drop.sql");
        File.Delete(dropPath);
        using (var writer = File.CreateText(dropPath))
        {
            SubscriptionScriptBuilder.BuildCreateScript(writer, sqlVariant);
        }
    }
}