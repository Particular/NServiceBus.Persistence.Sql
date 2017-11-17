#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace NServiceBus
{
    using System;

    public partial class SqlDialect
    {
        [Obsolete("Not for public use")]
        public abstract string GetSagaTableName(string tablePrefix, string tableSuffix);
        [Obsolete("Not for public use")]
        public abstract string BuildSelectFromCommand(string tableName);
        [Obsolete("Not for public use")]
        public abstract string BuildCompleteCommand(string tableName);
        [Obsolete("Not for public use")]
        public abstract string BuildGetBySagaIdCommand(string tableName);
        [Obsolete("Not for public use")]
        public abstract string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName);
        [Obsolete("Not for public use")]
        public abstract string BuildGetByPropertyCommand(string correlationProperty, string tableName);
        [Obsolete("Not for public use")]
        public abstract string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName);

        internal virtual object BuildSagaData(CommandWrapper command, RuntimeSagaInfo sagaInfo, IContainSagaData sagaData)
        {
            return sagaInfo.ToJson(sagaData);
        }

        internal virtual void ValidateJsonSettings(Newtonsoft.Json.JsonSerializer jsonSerializer)
        {

        }
    }
}