﻿namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Data.Common;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;

    /// <summary>
    /// Executes the scripts produced by a Sql Persistence MSBuild task.
    /// </summary>
    public static partial class ScriptRunner
    {
        static ILog log = LogManager.GetLogger(nameof(ScriptRunner));

        /// <summary>
        /// Executes the scripts produced by a Sql Persistence MSBuild task.
        /// </summary>
        /// <remarks>
        /// Designed to be used in a manual installation without the requirement of starting a full NServiceBus endpoint.
        /// </remarks>
        public static Task Install(SqlDialect sqlDialect, string tablePrefix, Func<DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = true, bool shouldInstallSagas = true, bool shouldInstallSubscriptions = true, CancellationToken cancellationToken = default) =>
            Install(sqlDialect, tablePrefix, _ => connectionBuilder(), scriptDirectory, shouldInstallOutbox, shouldInstallSagas, shouldInstallSubscriptions, cancellationToken);

        /// <summary>
        /// Executes the scripts produced by a Sql Persistence MSBuild task.
        /// </summary>
        /// <remarks>
        /// Designed to be used in a manual installation without the requirement of starting a full NServiceBus endpoint.
        /// </remarks>
        public static async Task Install(SqlDialect sqlDialect, string tablePrefix, Func<Type, DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = true, bool shouldInstallSagas = true, bool shouldInstallSubscriptions = true, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sqlDialect);
            ArgumentNullException.ThrowIfNull(tablePrefix);
            Guard.AgainstSqlDelimiters(nameof(tablePrefix), tablePrefix);
            ArgumentNullException.ThrowIfNull(connectionBuilder);
            ArgumentException.ThrowIfNullOrWhiteSpace(scriptDirectory);
            sqlDialect.ValidateTablePrefix(tablePrefix);

            if (shouldInstallOutbox)
            {
                await ExecuteInSeparateConnection<StorageType.Outbox>(InstallOutbox, scriptDirectory, tablePrefix, sqlDialect, connectionBuilder, cancellationToken).ConfigureAwait(false);
            }

            if (shouldInstallSagas)
            {
                await ExecuteInSeparateConnection<StorageType.Sagas>(InstallSagas, scriptDirectory, tablePrefix, sqlDialect, connectionBuilder, cancellationToken).ConfigureAwait(false);
            }

            if (shouldInstallSubscriptions)
            {
                await ExecuteInSeparateConnection<StorageType.Subscriptions>(InstallSubscriptions, scriptDirectory, tablePrefix, sqlDialect, connectionBuilder, cancellationToken).ConfigureAwait(false);
            }
        }

        static async Task ExecuteInSeparateConnection<T>(Func<string, DbConnection, DbTransaction, string, SqlDialect, CancellationToken, Task> installAction, string scriptDirectory, string tablePrefix, SqlDialect sqlDialect, Func<Type, DbConnection> connectionBuilder, CancellationToken cancellationToken)
            where T : StorageType
        {
            using (var connection = connectionBuilder(typeof(T)))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                {
                    await installAction(scriptDirectory, connection, transaction, tablePrefix, sqlDialect, cancellationToken).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        static Task InstallOutbox(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect, CancellationToken cancellationToken)
        {
            var createScript = Path.Combine(scriptDirectory, "Outbox_Create.sql");
            ScriptLocation.ValidateScriptExists(createScript);
            log.Info($"Executing '{createScript}'");

            return sqlDialect.ExecuteTableCommand(
                connection: connection,
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix,
                cancellationToken);
        }

        static Task InstallSubscriptions(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect, CancellationToken cancellationToken)
        {
            var createScript = Path.Combine(scriptDirectory, "Subscription_Create.sql");
            ScriptLocation.ValidateScriptExists(createScript);
            log.Info($"Executing '{createScript}'");

            return sqlDialect.ExecuteTableCommand(
                connection: connection,
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix,
                cancellationToken);
        }

        static async Task InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect, CancellationToken cancellationToken)
        {
            var sagasDirectory = Path.Combine(scriptDirectory, "Sagas");
            if (!Directory.Exists(sagasDirectory))
            {
                log.Info($"Directory '{sagasDirectory}' not found so no saga creation scripts will be executed.");
                return;
            }

            var scriptFiles = Directory.EnumerateFiles(sagasDirectory, "*_Create.sql").ToList();
            log.Info($@"Executing saga creation scripts:
{string.Join(Environment.NewLine, scriptFiles)}");
            var sagaScripts = scriptFiles
                .Select(File.ReadAllText);

            foreach (var script in sagaScripts)
            {
                await sqlDialect.ExecuteTableCommand(connection, transaction, script, tablePrefix, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}