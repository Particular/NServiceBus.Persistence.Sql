namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;
    using Unicast.Subscriptions;

    class TimeoutSettingDialect : SqlDialect
    {
        SqlDialect impl;
        int commandTimeout;

        public TimeoutSettingDialect(SqlDialect impl, int commandTimeout)
        {
            this.impl = impl;
            this.commandTimeout = commandTimeout;
        }

        internal override Task<StorageSession> TryAdaptTransportConnection(TransportTransaction transportTransaction,
            ContextBag context,
            IConnectionManager connectionManager,
            Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory,
            CancellationToken cancellationToken = default) =>
            impl.TryAdaptTransportConnection(transportTransaction, context, connectionManager, storageSessionFactory, cancellationToken);

        internal override string GetSubscriptionTableName(string tablePrefix)
        {
            return impl.GetSubscriptionTableName(tablePrefix);
        }

        internal override string GetSubscriptionSubscribeCommand(string tableName)
        {
            return impl.GetSubscriptionSubscribeCommand(tableName);
        }

        internal override string GetSubscriptionUnsubscribeCommand(string tableName)
        {
            return impl.GetSubscriptionUnsubscribeCommand(tableName);
        }

        internal override Func<List<MessageType>, string> GetSubscriptionQueryFactory(string tableName)
        {
            return impl.GetSubscriptionQueryFactory(tableName);
        }

        internal override string GetSagaTableName(string tablePrefix, string tableSuffix)
        {
            return impl.GetSagaTableName(tablePrefix, tableSuffix);
        }

        internal override Func<string, string> BuildSelectFromCommand(string tableName)
        {
            return impl.BuildSelectFromCommand(tableName);
        }

        internal override string BuildCompleteCommand(string tableName)
        {
            return impl.BuildCompleteCommand(tableName);
        }

        internal override string BuildGetBySagaIdCommand(string tableName)
        {
            return impl.BuildGetBySagaIdCommand(tableName);
        }

        internal override string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName)
        {
            return impl.BuildSaveCommand(correlationProperty, transitionalCorrelationProperty, tableName);
        }

        internal override string BuildGetByPropertyCommand(string correlationProperty, string tableName)
        {
            return impl.BuildGetByPropertyCommand(correlationProperty, tableName);
        }

        internal override string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName)
        {
            return impl.BuildUpdateCommand(transitionalCorrelationProperty, tableName);
        }

        internal override string GetOutboxTableName(string tablePrefix)
        {
            return impl.GetOutboxTableName(tablePrefix);
        }

        internal override string GetOutboxSetAsDispatchedCommand(string tableName)
        {
            return impl.GetOutboxSetAsDispatchedCommand(tableName);
        }

        internal override string GetOutboxGetCommand(string tableName)
        {
            return impl.GetOutboxGetCommand(tableName);
        }

        internal override string GetOutboxOptimisticStoreCommand(string tableName)
        {
            return impl.GetOutboxOptimisticStoreCommand(tableName);
        }

        internal override string GetOutboxPessimisticBeginCommand(string tableName)
        {
            return impl.GetOutboxPessimisticBeginCommand(tableName);
        }

        internal override string GetOutboxPessimisticCompleteCommand(string tableName)
        {
            return impl.GetOutboxPessimisticCompleteCommand(tableName);
        }

        internal override string GetOutboxCleanupCommand(string tableName)
        {
            return impl.GetOutboxCleanupCommand(tableName);
        }

        internal override void SetJsonParameterValue(DbParameter parameter, object value)
        {
            impl.SetJsonParameterValue(parameter, value);
        }

        internal override void SetParameterValue(DbParameter parameter, object value)
        {
            impl.SetParameterValue(parameter, value);
        }

        internal override CommandWrapper CreateCommand(DbConnection connection)
        {
            var commandWrapper = impl.CreateCommand(connection);
            commandWrapper.InnerCommand.CommandTimeout = commandTimeout;
            return commandWrapper;
        }

        internal override CommandBehavior ModifyBehavior(DbConnection connection, CommandBehavior baseBehavior)
        {
            return impl.ModifyBehavior(connection, baseBehavior);
        }

        internal override object GetCustomDialectDiagnosticsInfo()
        {
            return impl.GetCustomDialectDiagnosticsInfo();
        }
    }
}