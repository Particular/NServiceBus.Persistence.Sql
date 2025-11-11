using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

class OutboxInstaller(IReadOnlySettings settings) : INeedToInstallSomething
{
    public async Task Install(string identity, CancellationToken cancellationToken = default)
    {
        Func<Type, DbConnection> connectionBuilder = storageType => settings.GetConnectionBuilder(storageType).BuildNonContextual();
        var dialect = settings.GetSqlDialect();
        var tablePrefix = settings.GetTablePrefix(settings.EndpointName());

        dialect.ValidateTablePrefix(tablePrefix);

        await ScriptRunner.Install(
                sqlDialect: dialect,
                tablePrefix: tablePrefix,
                connectionBuilder: connectionBuilder,
                scriptDirectory: ScriptLocation.FindScriptDirectory(settings),
                shouldInstallOutbox: true,
                shouldInstallSagas: false,
                shouldInstallSubscriptions: false,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}