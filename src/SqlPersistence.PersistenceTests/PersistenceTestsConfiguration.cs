namespace NServiceBus.PersistenceTesting;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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

            RegisterVariants(variants, DatabaseEngine.MsSqlServer);
        }

        var postgresConnectionString = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            RegisterVariants(variants, DatabaseEngine.Postgres);
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MySQLConnectionString")))
        {
            RegisterVariants(variants, DatabaseEngine.MySql);
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OracleConnectionString")))
        {
            RegisterVariants(variants, DatabaseEngine.Oracle);
        }

        SagaVariants = [.. variants];
        OutboxVariants = [.. variants];
    }

    static void RegisterVariants(List<object> variants, DatabaseEngine databaseEngine)
    {
        foreach (var adoIsolationLevel in databaseEngine.SupportedAdoIsolationLevels)
        {
            variants.Add(CreateVariant(databaseEngine, TransactionMode.Ado(adoIsolationLevel)));
        }

        foreach (var scopeIsolationLevel in databaseEngine.SupportedScopeIsolationLevels)
        {
            variants.Add(CreateVariant(databaseEngine, TransactionMode.Scope(scopeIsolationLevel)));
        }
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
            TablePrefix,
            dialect,
            SagaMetadataCollection,
            sagaName => SagaTableSuffix(buildDialect, sagaName));

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

            // Dropping and recreating every table for every fixture dominated the run time, so the schema is created once per engine and each fixture only empties it.
            lock (schemaLock)
            {
                if (createdSchemas.Add(buildDialect))
                {
                    CreateSchema(connection, buildDialect);
                }
            }

            ClearTables(connection, variant.DatabaseEngine.SqlDialect, buildDialect);
        }

        return Task.CompletedTask;

        DbConnection ConnectionFactory() => variant.Open();
    }

    // Saga tables come from every Saga type in this assembly, so a new saga or test needs no change here.
    void CreateSchema(DbConnection connection, BuildSqlDialect buildDialect)
    {
        foreach (var definition in GetSagaDefinitions(buildDialect))
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, buildDialect), "PersistenceTests");
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, buildDialect), "PersistenceTests");
        }

        connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(buildDialect), "PersistenceTests");
        connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(buildDialect), "PersistenceTests");
    }

    // Uses the table names the persistence itself resolves, so it covers exactly the tables CreateSchema made.
    void ClearTables(DbConnection connection, SqlDialect dialect, BuildSqlDialect buildDialect)
    {
        var tableNames = GetSagaDefinitions(buildDialect)
            .Select(definition => dialect.GetSagaTableName(TablePrefix, definition.TableSuffix))
            .Append(dialect.GetOutboxTableName(TablePrefix));

        foreach (var tableName in tableNames)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"delete from {tableName}";
            command.ExecuteNonQuery();
        }
    }

    IEnumerable<SagaDefinition> GetSagaDefinitions(BuildSqlDialect buildDialect)
    {
        foreach (var saga in SagaMetadataCollection)
        {
            CorrelationProperty correlationProperty = null;
            if (saga.TryGetCorrelationProperty(out var propertyMetadata))
            {
                correlationProperty = new CorrelationProperty(propertyMetadata.Name, CorrelationPropertyType.String);
            }

            yield return new SagaDefinition(SagaTableSuffix(buildDialect, saga.SagaType.Name), saga.EntityName, correlationProperty);
        }
    }

    static readonly object schemaLock = new();
    static readonly HashSet<BuildSqlDialect> createdSchemas = [];

    const string TablePrefix = "PersistenceTests_";

    static string SagaTableSuffix(BuildSqlDialect dialect, string sagaName) =>
        dialect == BuildSqlDialect.Oracle
            ? OracleSagaTableNames.Create(OracleSagaTableNames.PersistenceTestsPrefix, ShortenSagaName(sagaName))
            : ShortenSagaName(sagaName);

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
        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, TablePrefix);

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