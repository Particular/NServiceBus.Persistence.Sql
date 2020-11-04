using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Logging;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Executes the scripts produced by a Sql Persistence MSBuild task.
    /// </summary>
    public static partial class ScriptRunner
    {
        static ILog log = Logging.LogManager.GetLogger(nameof(ScriptRunner));

        /// <summary>
        /// Executes the scripts produced by a Sql Persistence MSBuild task.
        /// </summary>
        /// <remarks>
        /// Designed to be used in a manual installation without the requirement of starting a full NServiceBus endpoint.
        /// </remarks>
        public static Task Install(SqlDialect sqlDialect, string tablePrefix, Func<DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = true, bool shouldInstallSagas = true, bool shouldInstallSubscriptions = true)
        {
            return Install(sqlDialect, tablePrefix, x => connectionBuilder(), scriptDirectory, shouldInstallOutbox, shouldInstallSagas, shouldInstallSubscriptions);
        }

        /// <summary>
        /// Executes the scripts produced by a Sql Persistence MSBuild task.
        /// </summary>
        /// <remarks>
        /// Designed to be used in a manual installation without the requirement of starting a full NServiceBus endpoint.
        /// </remarks>
        public static async Task Install(SqlDialect sqlDialect, string tablePrefix, Func<Type, DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = true, bool shouldInstallSagas = true, bool shouldInstallSubscriptions = true)
        {
            Guard.AgainstNull(nameof(sqlDialect), sqlDialect);
            Guard.AgainstNull(nameof(tablePrefix), tablePrefix);
            Guard.AgainstSqlDelimiters(nameof(tablePrefix), tablePrefix);
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);
            Guard.AgainstNullAndEmpty(nameof(scriptDirectory), scriptDirectory);
            sqlDialect.ValidateTablePrefix(tablePrefix);

            if (shouldInstallOutbox)
            {
                await ExecuteInSeparateConnection<StorageType.Outbox>(InstallOutbox, scriptDirectory, tablePrefix, sqlDialect, connectionBuilder).ConfigureAwait(false);
            }

            if (shouldInstallSagas)
            {
                await ExecuteInSeparateConnection<StorageType.Sagas>(InstallSagas, scriptDirectory, tablePrefix, sqlDialect, connectionBuilder).ConfigureAwait(false);
            }

            if (shouldInstallSubscriptions)
            {
                await ExecuteInSeparateConnection<StorageType.Subscriptions>(InstallSubscriptions, scriptDirectory, tablePrefix, sqlDialect, connectionBuilder).ConfigureAwait(false);
            }
        }

        static async Task ExecuteInSeparateConnection<T>(Func<string, DbConnection, DbTransaction, string, SqlDialect, Task> installAction, string scriptDirectory, string tablePrefix, SqlDialect sqlDialect, Func<Type, DbConnection> connectionBuilder)
            where T : StorageType
        {
            using (var connection = connectionBuilder(typeof(T)))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = connection.BeginTransaction())
                {
                    await installAction(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        static Task InstallOutbox(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
        {
            var createScript = Path.Combine(scriptDirectory, "Outbox_Create.sql");
            ScriptLocation.ValidateScriptExists(createScript);
            log.Info($"Executing '{createScript}'");

            return sqlDialect.ExecuteTableCommand(
                connection: connection,
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix);
        }

        static Task InstallSubscriptions(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
        {
            var createScript = Path.Combine(scriptDirectory, "Subscription_Create.sql");
            ScriptLocation.ValidateScriptExists(createScript);
            log.Info($"Executing '{createScript}'");

            return sqlDialect.ExecuteTableCommand(
                connection: connection,
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix);
        }

        static async Task InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
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
                await sqlDialect.ExecuteTableCommand(connection, transaction, script, tablePrefix).ConfigureAwait(false);
            }
        }
    }
}