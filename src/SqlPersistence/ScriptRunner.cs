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
    public static class ScriptRunner
    {
        static ILog log = Logging.LogManager.GetLogger(nameof(ScriptRunner));

        /// <summary>
        /// Executes the scripts produced by a Sql Persistence MSBuild task.
        /// </summary>
        /// <remarks>
        /// Designed to be used in a manual installation without the requirement of starting a full NServiceBus endpoint.
        /// </remarks>
        public static async Task Install(SqlDialect sqlDialect, string tablePrefix, Func<DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = true, bool shouldInstallSagas = true, bool shouldInstallSubscriptions = true, bool shouldInstallTimeouts = true)
        {
            Guard.AgainstNull(nameof(sqlDialect), sqlDialect);
            Guard.AgainstNull(nameof(tablePrefix), tablePrefix);
            Guard.AgainstSqlDelimiters(nameof(tablePrefix), tablePrefix);
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);
            Guard.AgainstNullAndEmpty(nameof(scriptDirectory), scriptDirectory);
            sqlDialect.ValidateTablePrefix(tablePrefix);

            using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
            using (var transaction = connection.BeginTransaction())
            {
                if (shouldInstallOutbox)
                {
                    await InstallOutbox(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
                }

                if (shouldInstallSagas)
                {
                    await InstallSagas(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
                }

                if (shouldInstallSubscriptions)
                {
                    await InstallSubscriptions(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
                }

                if (shouldInstallTimeouts)
                {
                    await InstallTimeouts(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
                }

                transaction.Commit();
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

        static Task InstallTimeouts(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
        {
            var createScript = Path.Combine(scriptDirectory, "Timeout_Create.sql");
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