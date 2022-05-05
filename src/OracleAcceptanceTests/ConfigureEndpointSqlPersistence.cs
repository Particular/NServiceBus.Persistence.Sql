using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Settings;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        if (configuration.IsSendOnly())
        {
            return Task.CompletedTask;
        }

        var lastDot = endpointName.LastIndexOf('.');
        if (lastDot > 0)
        {
            endpointName = endpointName.Substring(lastDot + 1) + Math.Abs(endpointName.GetHashCode());
        }
        var tablePrefix = TableNameCleaner.Clean(endpointName).Substring(0, Math.Min(endpointName.Length, 24));
        Console.WriteLine($"Using EndpointName='{endpointName}', TablePrefix='{tablePrefix}'");
        configuration.RegisterStartupTask(sp => new SetupAndTeardownDatabase(
            sp.GetRequiredService<IReadOnlySettings>(),
            tablePrefix,
            OracleConnectionBuilder.Build,
            BuildSqlDialect.Oracle,
            FilterTableExists));

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.Oracle>();
        persistence.ConnectionBuilder(OracleConnectionBuilder.Build);
        persistence.TablePrefix($"{tablePrefix}_");
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();

        //Force Saga table names to 27 characters to fit in Oracle
        var sagaSettings = persistence.SagaSettings();
        sagaSettings.NameFilter(sagaName => sagaName.Substring(0, Math.Min(27, sagaName.Length)));

        return Task.CompletedTask;
    }

    bool FilterTableExists(Exception exception)
    {
        return exception.Message.Contains("ORA-00054") || // resource busy and acquire with NOWAIT specified or timeout expired
            exception.Message.Contains("ORA-00942"); // table or view does not exist
    }
    public Task Cleanup() =>
        //Cleanup is made in the SetupAndTeardownDatabase feature OnStop method 
        Task.CompletedTask;
}