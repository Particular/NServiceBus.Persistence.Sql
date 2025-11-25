using NServiceBus.Persistence.Sql.ScriptBuilder;

class SubscriptionWriter(bool clean, bool overwrite, string scriptPath) : ScriptWriter(clean, overwrite, scriptPath)
{
    public override void WriteScripts(BuildSqlDialect dialect)
    {
        WriteScript("Subscription_Create.sql", writer => SubscriptionScriptBuilder.BuildCreateScript(writer, dialect));
        WriteScript("Subscription_Drop.sql", writer => SubscriptionScriptBuilder.BuildDropScript(writer, dialect));
    }
}