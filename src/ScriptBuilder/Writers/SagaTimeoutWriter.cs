using NServiceBus.Persistence.Sql.ScriptBuilder;

class SagaTimeoutWriter(bool clean, bool overwrite, string scriptPath) : ScriptWriter(clean, overwrite, scriptPath)
{
    public override void WriteScripts(BuildSqlDialect dialect)
    {
        WriteScript("SagaTimeout_Create.sql", writer => SagaTimeoutScriptBuilder.BuildCreateScript(writer, dialect));
        WriteScript("SagaTimeout_Drop.sql", writer => SagaTimeoutScriptBuilder.BuildDropScript(writer, dialect));
    }
}