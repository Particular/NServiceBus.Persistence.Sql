using NServiceBus.Persistence.Sql.ScriptBuilder;

class OutboxWriter : ScriptWriter
{
    public OutboxWriter(bool clean, bool overwrite, string scriptPath)
        : base(clean, overwrite, scriptPath)
    {
    }

    public override void WriteScripts(BuildSqlDialect dialect)
    {
        WriteScript("Outbox_Create.sql", writer => OutboxScriptBuilder.BuildCreateScript(writer, dialect));

        WriteScript("Outbox_Drop.sql", writer => OutboxScriptBuilder.BuildDropScript(writer, dialect));
    }
}