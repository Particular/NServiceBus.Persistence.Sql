namespace NServiceBus.PersistenceTesting;

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Sagas;
using NServiceBus.Transport;
using NUnit.Framework;

public partial class PersistenceTestsConfiguration
{
    public bool SupportsDtc => OperatingSystem.IsWindows() && ((SqlTestVariant)Variant.Values[0]).SupportsDtc;

    public bool SupportsOutbox => true;

    public bool SupportsFinders => false;

    public bool SupportsPessimisticConcurrency => true;

    public ISagaIdGenerator SagaIdGenerator { get; private set; }

    public ISagaPersister SagaStorage { get; private set; }

    public IOutboxStorage OutboxStorage { get; private set; }

    public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

    static PersistenceTestsConfiguration()
    {
        var variants = new List<object>();

        var sqlServerConnectionString = Environment.GetEnvironmentVariable("SQLServerConnectionString");
        if (!string.IsNullOrWhiteSpace(sqlServerConnectionString))
        {
            using var connection = new SqlConnection(sqlServerConnectionString);

            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $"ALTER DATABASE {connection.Database} SET ALLOW_SNAPSHOT_ISOLATION ON";
            _ = command.ExecuteNonQuery();

            RegisterCommonVariants(variants, new SqlDialect.MsSqlServer(), BuildSqlDialect.MsSqlServer, supportsDtc: true);

            variants.Add(CreateVariant(new SqlDialect.MsSqlServer(),
                BuildSqlDialect.MsSqlServer,
                usePessimisticModeForOutbox: false,
                supportsDtc: true,
                isolationLevel: IsolationLevel.Snapshot));
        }

        var postgresConnectionString = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            RegisterCommonVariants(variants, new SqlDialect.PostgreSql(), BuildSqlDialect.PostgreSql);

            variants.Add(CreateVariant(new SqlDialect.PostgreSql(),
                BuildSqlDialect.PostgreSql,
                usePessimisticModeForOutbox: false,
                isolationLevel: IsolationLevel.Snapshot));
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MySQLConnectionString")))
        {
            RegisterCommonVariants(variants, new SqlDialect.MySql(), BuildSqlDialect.MySql);
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OracleConnectionString")))
        {
            RegisterCommonVariants(variants, new SqlDialect.Oracle(), BuildSqlDialect.Oracle);
        }

        SagaVariants = [.. variants];
        OutboxVariants = [.. variants];
    }

    static void RegisterCommonVariants(List<object> variants, SqlDialect sqlDialect, BuildSqlDialect buildSqlDialect, bool supportsDtc = false)
    {
        variants.Add(CreateVariant(sqlDialect,
            buildSqlDialect,
            usePessimisticModeForOutbox: false,
            supportsDtc: supportsDtc,
            isolationLevel: IsolationLevel.ReadCommitted));
    }

    static TestFixtureData CreateVariant(SqlDialect dialect,
        BuildSqlDialect buildDialect,
        bool usePessimisticModeForOutbox = false,
        bool supportsDtc = false,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        bool useTransactionScope = false,
        System.Transactions.IsolationLevel scopeIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted) =>
        new(new TestVariant(new SqlTestVariant(dialect, buildDialect, usePessimisticModeForOutbox, supportsDtc, isolationLevel, useTransactionScope, scopeIsolationLevel)));

    public Task Configure(CancellationToken cancellationToken = default)
    {
        var variant = (SqlTestVariant)Variant.Values[0];
        var dialect = variant.Dialect;
        var buildDialect = variant.BuildDialect;
        var connectionFactory = () => variant.Open();
        var isolationLevel = variant.IsolationLevel;
        var scopeIsolationLevel = variant.ScopeIsolationLevel;
        var useTransactionScopeScope = variant.UseTransactionScope;
        var usePessimisticModeForOutbox = variant.UsePessimisticModeForOutbox;

        if (SessionTimeout.HasValue)
        {
            dialect = new TimeoutSettingDialect(dialect, (int)SessionTimeout.Value.TotalSeconds);
        }

        var infoCache = new SagaInfoCache(
            null,
            Serializer.JsonSerializer,
            reader => new JsonTextReader(reader),
            writer => new JsonTextWriter(writer),
            "PersistenceTests_",
            dialect,
            SagaMetadataCollection,
            ShortenSagaName);

        var connectionManager = new ConnectionManager(connectionFactory);
        SagaIdGenerator = new DefaultSagaIdGenerator();
        SagaStorage = new SagaPersister(infoCache, dialect);
        OutboxStorage = CreateOutboxPersister(connectionManager, dialect, usePessimisticModeForOutbox, useTransactionScopeScope, isolationLevel, scopeIsolationLevel);
        CreateStorageSession = () => new StorageSession(connectionManager, infoCache, dialect);

        GetContextBagForSagaStorage = () =>
        {
            var contextBag = new ContextBag();
            contextBag.Set(new IncomingMessage("MessageId", [], Array.Empty<byte>()));
            return contextBag;
        };

        GetContextBagForOutbox = () =>
        {
            var contextBag = new ContextBag();
            contextBag.Set(new IncomingMessage("MessageId", [], Array.Empty<byte>()));
            return contextBag;
        };

        using (var connection = connectionFactory())
        {
            connection.Open();

            foreach (var saga in SagaMetadataCollection)
            {
                CorrelationProperty correlationProperty = null;
                if (saga.TryGetCorrelationProperty(out var propertyMetadata))
                {
                    correlationProperty = new CorrelationProperty(propertyMetadata.Name, CorrelationPropertyType.String);
                }

                var tableName = ShortenSagaName(saga.SagaType.Name);
                var definition = new SagaDefinition(tableName, saga.EntityName, correlationProperty);

                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, buildDialect), "PersistenceTests");
                connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, buildDialect), "PersistenceTests");
            }

            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(buildDialect), "PersistenceTests");
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(buildDialect), "PersistenceTests");
        }

        return Task.CompletedTask;
    }

    static string ShortenSagaName(string sagaName) =>
        sagaName
            .Replace("AnotherSagaWithCorrelatedProperty", "ASWCP")
            .Replace("SagaWithCorrelationProperty", "SWCP")
            .Replace("SagaWithoutCorrelationProperty", "SWOCP")
            .Replace("SagaWithComplexType", "SWCT")
            .Replace("TestSaga", "TS");

    static OutboxPersister CreateOutboxPersister(IConnectionManager connectionManager,
        SqlDialect sqlDialect,
        bool pessimisticMode,
        bool transactionScopeMode,
        IsolationLevel isolationLevel,
        System.Transactions.IsolationLevel scopeIsolationLevel)
    {
        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, "PersistenceTests_");
        ConcurrencyControlStrategy concurrencyControlStrategy;
        if (pessimisticMode)
        {
            concurrencyControlStrategy = new PessimisticConcurrencyControlStrategy(sqlDialect, outboxCommands);
        }
        else
        {
            concurrencyControlStrategy = new OptimisticConcurrencyControlStrategy(sqlDialect, outboxCommands);
        }

        return new OutboxPersister(connectionManager, sqlDialect, outboxCommands, TransactionFactory);

        ISqlOutboxTransaction TransactionFactory() => transactionScopeMode
            ? new TransactionScopeSqlOutboxTransaction(concurrencyControlStrategy, connectionManager, scopeIsolationLevel, TimeSpan.Zero)
            : new AdoNetSqlOutboxTransaction(concurrencyControlStrategy, connectionManager, isolationLevel);
    }

    public Task Cleanup(CancellationToken cancellationToken = default) => Task.CompletedTask;
}