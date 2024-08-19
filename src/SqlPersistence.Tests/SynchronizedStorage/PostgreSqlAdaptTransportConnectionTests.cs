namespace PostgreSql
{
    using System;
    using System.Data.Common;
    using NServiceBus.Extensibility;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [PostgreSqlTest]
    class PostgreSqlAdaptTransportConnectionTests
    {
        readonly BuildSqlDialect sqlDialect = BuildSqlDialect.PostgreSql;
        readonly IConnectionManager connectionManager = new ConnectionManager(() =>
        {
            var connection = PostgreSqlConnectionBuilder.Build();
            connection.Open();

            return connection;
        });

        [Test]
        public void It_throws_if_transport_transaction_scope_exists()
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                var ex = Assert.ThrowsAsync<Exception>(async () =>
                {
                    var transportTransaction = new TransportTransaction();
                    transportTransaction.Set(Transaction.Current);

                    await sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(),
                        connectionManager);
                });

                StringAssert.StartsWith("PostgreSQL persistence should not be used with TransportTransactionMode equal to TransactionScope", ex.Message);
            }
        }

        [Test]
        public async Task It_returns_false_if_there_is_no_transaction_scope()
        {
            var transportTransaction = new TransportTransaction();
            (bool wasAdapted, DbConnection _, DbTransaction _, bool _) = await sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(), connectionManager);

            Assert.That(wasAdapted, Is.False);
        }

        [Test]
        public async Task Adapts_transport_connection()
        {
            var transportTransaction = new TransportTransaction();

            var transportConnection = connectionManager.BuildNonContextual();
            var transaction = transportConnection.BeginTransaction();

            transportTransaction.Set("System.Data.SqlClient.SqlConnection", transportConnection);
            transportTransaction.Set("System.Data.SqlClient.SqlTransaction", transaction);

            var altConnectionManager = new ConnectionManager(() => throw new Exception("Should not be called"));

            (bool wasAdapted, DbConnection connection, DbTransaction tx, bool ownsTransaction) =
                await sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(), altConnectionManager);

            Assert.Multiple(() =>
            {
                Assert.That(wasAdapted, Is.True);
                Assert.That(connection, Is.EqualTo(transportConnection));
                Assert.That(tx, Is.EqualTo(transaction));
                Assert.That(ownsTransaction, Is.False);
            });
        }
    }
}