namespace NServiceBus
{
    public partial class SqlDialect
    {
        internal abstract string GetSagaTableName(string tablePrefix, string tableSuffix);
        internal abstract string BuildSelectFromCommand(string tableName);
        internal abstract string BuildCompleteCommand(string tableName);
        internal abstract string BuildGetBySagaIdCommand(string tableName);
        internal abstract string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName);
        internal abstract string BuildGetByPropertyCommand(string correlationProperty, string tableName);
        internal abstract string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName);

        internal virtual object BuildSagaData(CommandWrapper command, RuntimeSagaInfo sagaInfo, IContainSagaData sagaData)
        {
            return sagaInfo.ToJson(sagaData);
        }
    }
}