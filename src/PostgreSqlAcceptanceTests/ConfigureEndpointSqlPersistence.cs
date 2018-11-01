using System;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    ConfigureEndpointHelper endpointHelper;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        if (configuration.IsSendOnly())
        {
            return Task.FromResult(0);
        }

        var hashcodeString = Math.Abs(endpointName.GetHashCode()).ToString();
        var suffixLength = 19 - hashcodeString.Length;
        var nameSuffix = endpointName.Substring(Math.Max(0, endpointName.Length - suffixLength));
        endpointName = nameSuffix + hashcodeString;
        var tablePrefix = TableNameCleaner.Clean(endpointName).Substring(0, Math.Min(endpointName.Length, 19));
        Console.WriteLine($"Using EndpointName='{endpointName}', TablePrefix='{tablePrefix}'");

        endpointHelper = new ConfigureEndpointHelper(configuration, tablePrefix, PostgreSqlConnectionBuilder.Build, BuildSqlDialect.PostgreSql, FilterTableExists);
        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(context => PostgreSqlConnectionBuilder.Build());
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
        return Task.FromResult(0);
    }

    bool FilterTableExists(Exception exception)
    {
        return exception.Message.Contains("Cannot drop the table");
    }

    public Task Cleanup()
    {
        return endpointHelper?.Cleanup();
    }
}