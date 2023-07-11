using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Settings;
using NUnit.Framework;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    SetupAndTeardownDatabase setupFeature;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        if (configuration.IsSendOnly())
        {
            return Task.CompletedTask;
        }

        //Why is it 19? Answer: because we constrain the tablePrefix in PostgreSQL to 20 and we add '_' to the prefix later on
        var tablePrefix = TestTableNameCleaner.Clean(endpointName, 19);
        TestContext.WriteLine($"Using EndpointName='{endpointName}', TablePrefix='{tablePrefix}'");

        configuration.RegisterStartupTask(sp =>
        {
            setupFeature = new SetupAndTeardownDatabase(
                TestContext.CurrentContext.Test.ID,
                sp.GetRequiredService<IReadOnlySettings>(),
                tablePrefix,
                AuroraMySqlConnectionBuilder.Build,
                BuildSqlDialect.PostgreSql,
                e => e.Message.Contains("duplicate key value violates unique constraint"));

            return setupFeature;
        });

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(AuroraMySqlConnectionBuilder.Build);
        var sqlDialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
        persistence.TablePrefix($"{tablePrefix}_");
        sqlDialect.JsonBParameterModifier(parameter =>
        {
            var npgsqlParameter = (NpgsqlParameter)parameter;
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
        });

        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();
        return Task.CompletedTask;
    }

    public Task Cleanup() => setupFeature != null ? setupFeature.ManualStop(CancellationToken.None) : Task.CompletedTask;
}