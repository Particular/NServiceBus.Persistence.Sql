namespace NServiceBus
{
    using System;

    public partial class SqlDialect
    {
        internal abstract string GetSagaTableName(string tablePrefix, string tableSuffix);

        internal abstract Func<string, string> BuildSelectFromCommand(string tableName, bool usesOptimisticConcurrency);

        internal abstract string BuildCompleteCommand(string tableName);

        internal abstract string BuildGetBySagaIdCommand(string tableName, bool usesOptimisticConcurrency);

        internal abstract string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName);

        internal abstract string BuildGetByPropertyCommand(string correlationProperty, string tableName, bool usesOptimisticConcurrency);

        internal abstract string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName);

        internal virtual object BuildSagaData(CommandWrapper command, RuntimeSagaInfo sagaInfo, IContainSagaData sagaData)
        {
            return sagaInfo.ToJson(sagaData);
        }

        internal virtual void ValidateJsonSettings(Newtonsoft.Json.JsonSerializer jsonSerializer)
        {

        }
    }
}