namespace NServiceBus.PersistenceTesting;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
    public bool SupportsDtc => OperatingSystem.IsWindows() && ((SqlTestVariant)Variant.Values[0]).DatabaseEngine.SupportsDtc;

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

            RegisterCommonVariants(variants, DatabaseEngine.MsSqlServer);

            variants.Add(CreateVariant(DatabaseEngine.MsSqlServer, TransactionMode.Ado(IsolationLevel.Snapshot)));
            variants.Add(CreateVariant(DatabaseEngine.MsSqlServer, TransactionMode.Scope(System.Transactions.IsolationLevel.Snapshot)));
        }

        var postgresConnectionString = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            RegisterCommonVariants(variants, DatabaseEngine.Postgres);

            variants.Add(CreateVariant(DatabaseEngine.Postgres, TransactionMode.Ado(IsolationLevel.Snapshot)));
            variants.Add(CreateVariant(DatabaseEngine.Postgres, TransactionMode.Scope(System.Transactions.IsolationLevel.Snapshot)));
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MySQLConnectionString")))
        {
            RegisterCommonVariants(variants, DatabaseEngine.MySql);
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OracleConnectionString")))
        {
            RegisterCommonVariants(variants, DatabaseEngine.Oracle);
        }

        SagaVariants = [.. variants];
        OutboxVariants = [.. variants];
    }

    static void RegisterCommonVariants(List<object> variants, DatabaseEngine databaseEngine)
    {
        variants.Add(CreateVariant(databaseEngine, TransactionMode.Ado(IsolationLevel.ReadCommitted)));
        variants.Add(CreateVariant(databaseEngine, TransactionMode.Scope(System.Transactions.IsolationLevel.Serializable)));
    }

    // OutboxLockMode must always be set to Optimistic until the core persistence tests have been modified
    // to take pessimistic outbox locking into account - https://github.com/Particular/NServiceBus/issues/7237
    static TestFixtureData CreateVariant(DatabaseEngine databaseEngine,
        TransactionMode transactionMode,
        OutboxLockMode outboxLockMode = OutboxLockMode.Optimistic
    ) =>
        new(new TestVariant(new SqlTestVariant(databaseEngine, transactionMode, outboxLockMode)));

    public Task Configure(CancellationToken cancellationToken = default)
    {
        var variant = (SqlTestVariant)Variant.Values[0];
        var dialect = variant.DatabaseEngine.SqlDialect;
        var buildDialect = variant.DatabaseEngine.BuildSqlDialect;

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

        var connectionManager = new ConnectionManager(ConnectionFactory);
        SagaIdGenerator = new DefaultSagaIdGenerator();
        SagaStorage = new SagaPersister(infoCache, dialect);
        OutboxStorage = CreateOutboxPersister(connectionManager, dialect, variant.TransactionMode, variant.OutboxLockMode);
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

        using (var connection = ConnectionFactory())
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

        DbConnection ConnectionFactory() => variant.Open();
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
        TransactionMode transactionMode,
        OutboxLockMode outboxLockMode)
    {
        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, "PersistenceTests_");

        ConcurrencyControlStrategy concurrencyControlStrategy = outboxLockMode switch
        {
            OutboxLockMode.Optimistic => new OptimisticConcurrencyControlStrategy(sqlDialect, outboxCommands),
            OutboxLockMode.Pessimistic => new PessimisticConcurrencyControlStrategy(sqlDialect, outboxCommands),
            _ => throw new ArgumentOutOfRangeException(nameof(outboxLockMode), outboxLockMode, "Unknown outbox lock mode.")
        };

        var transactionScopeMode = transactionMode is TransactionScopeMode;

        return new OutboxPersister(connectionManager, sqlDialect, outboxCommands, TransactionFactory);

        ISqlOutboxTransaction TransactionFactory() => transactionScopeMode
            ? new TransactionScopeSqlOutboxTransaction(concurrencyControlStrategy,
                connectionManager,
                ((TransactionScopeMode)transactionMode).IsolationLevel,
                TimeSpan.Zero)
            : new AdoNetSqlOutboxTransaction(concurrencyControlStrategy,
                connectionManager,
                ((AdoTransactionMode)transactionMode).IsolationLevel);
    }

    public Task Cleanup(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
