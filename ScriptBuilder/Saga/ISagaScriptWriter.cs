using NServiceBus.Persistence.Sql.ScriptBuilder;

interface ISagaScriptWriter
{
    void WriteTableNameVariable();
    void WriteDropTable();
    void WritePurgeObsoleteProperties();
    void WriteCreateTable();
    void AddProperty(CorrelationProperty sagaCorrelationProperty);
    void VerifyColumnType(CorrelationProperty sagaCorrelationProperty);
    void WriteCreateIndex(CorrelationProperty sagaCorrelationProperty);
    void WritePurgeObsoleteIndex();
}