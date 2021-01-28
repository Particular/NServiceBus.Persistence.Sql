using NServiceBus.Persistence.Sql.ScriptBuilder;

class TimeoutWriter : ScriptWriter
{
    public TimeoutWriter(bool clean, bool overwrite, string scriptPath)
        : base(clean, overwrite, scriptPath)
    {
    }

    public override void WriteScripts(BuildSqlDialect dialect)
    {
        WriteScript("Timeout_Create.sql", writer => TimeoutScriptBuilder.BuildCreateScript(writer, dialect));

        WriteScript("Timeout_Drop.sql", writer => TimeoutScriptBuilder.BuildDropScript(writer, dialect));
    }
}