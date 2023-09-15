namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
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

        public bool SupportsPessimisticConcurrency { get; set; }

        public ISagaIdGenerator SagaIdGenerator { get; private set; }

        public ISagaPersister SagaStorage { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

        static PersistenceTestsConfiguration()
        {
            var variants = new List<object>();

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQLServerConnectionString")))
            {
                variants.Add(CreateVariant(new SqlDialect.MsSqlServer(), BuildSqlDialect.MsSqlServer, supportsDtc: true));
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PostgreSqlConnectionString")))
            {
                variants.Add(CreateVariant(new SqlDialect.PostgreSql(), BuildSqlDialect.PostgreSql));
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MySQLConnectionString")))
            {
                variants.Add(CreateVariant(new SqlDialect.MySql(), BuildSqlDialect.MySql));
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OracleConnectionString")))
            {
                variants.Add(CreateVariant(new SqlDialect.Oracle(), BuildSqlDialect.Oracle));
            }

            SagaVariants = variants.ToArray();
            OutboxVariants = variants.ToArray();
        }

        static TestFixtureData CreateVariant(SqlDialect dialect, BuildSqlDialect buildDialect, bool usePessimisticMode = true, bool supportsDtc = false) =>
            new TestFixtureData(new TestVariant(new SqlTestVariant(dialect, buildDialect, usePessimisticMode, supportsDtc)));

        public Task Configure(CancellationToken cancellationToken = default)
        {
            if (OperatingSystem.IsWindows() && SupportsDtc)
            {
                TransactionManager.ImplicitDistributedTransactions = true;
            }

            var variant = (SqlTestVariant)Variant.Values[0];
            var dialect = variant.Dialect;
            var buildDialect = variant.BuildDialect;
            var connectionFactory = () => variant.Open();
            var pessimisticMode = variant.UsePessimisticMode;

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
                name => ShortenSagaName(name));

            var connectionManager = new ConnectionManager(connectionFactory);
            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new SagaPersister(infoCache, dialect);
            OutboxStorage = CreateOutboxPersister(connectionManager, dialect, false, false);
            SupportsPessimisticConcurrency = pessimisticMode;
            CreateStorageSession = () => new StorageSession(connectionManager, infoCache, dialect);

            GetContextBagForSagaStorage = () =>
            {
                var contextBag = new ContextBag();
                contextBag.Set(new IncomingMessage("MessageId", new Dictionary<string, string>(), Array.Empty<byte>()));
                return contextBag;
            };

            GetContextBagForOutbox = () =>
            {
                var contextBag = new ContextBag();
                contextBag.Set(new IncomingMessage("MessageId", new Dictionary<string, string>(), Array.Empty<byte>()));
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

        static string ShortenSagaName(string sagaName)
        {
            return sagaName
                .Replace("AnotherSagaWithCorrelatedProperty", "ASWCP")
                .Replace("SagaWithCorrelationProperty", "SWCP")
                .Replace("SagaWithoutCorrelationProperty", "SWOCP")
                .Replace("SagaWithComplexType", "SWCT")
                .Replace("TestSaga", "TS");
        }

        static OutboxPersister CreateOutboxPersister(IConnectionManager connectionManager, SqlDialect sqlDialect, bool pessimisticMode, bool transactionScopeMode)
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

            ISqlOutboxTransaction transactionFactory()
            {
                return transactionScopeMode
                   ? new TransactionScopeSqlOutboxTransaction(concurrencyControlStrategy, connectionManager, IsolationLevel.ReadCommitted, TimeSpan.Zero)
                   : new AdoNetSqlOutboxTransaction(concurrencyControlStrategy, connectionManager, System.Data.IsolationLevel.ReadCommitted);
            }

            var outboxPersister = new OutboxPersister(connectionManager, sqlDialect, outboxCommands, transactionFactory);
            return outboxPersister;
        }

        public Task Cleanup(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}