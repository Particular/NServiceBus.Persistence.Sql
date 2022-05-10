using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
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

        var hashcodeString = Math.Abs(endpointName.GetHashCode()).ToString();
        var suffixLength = 19 - hashcodeString.Length;
        var nameSuffix = endpointName.Substring(Math.Max(0, endpointName.Length - suffixLength));
        endpointName = nameSuffix + hashcodeString;
        var tablePrefix = TableNameCleaner.Clean(endpointName).Substring(0, Math.Min(endpointName.Length, 19));
        Console.WriteLine($"Using EndpointName='{endpointName}', TablePrefix='{tablePrefix}'");

        configuration.RegisterStartupTask(sp => new SetupAndTeardownDatabase(
            sp.GetRequiredService<IReadOnlySettings>(),
            tablePrefix,
            PostgreSqlConnectionBuilder.Build,
            BuildSqlDialect.PostgreSql,
            e => e.Message.Contains("duplicate key value violates unique constraint")));

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(PostgreSqlConnectionBuilder.Build);
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

    public Task Cleanup() =>
        //Cleanup is made in the SetupAndTeardownDatabase feature OnStop method 
        Task.CompletedTask;
}