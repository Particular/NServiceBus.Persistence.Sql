namespace SqlServerSystemData
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [SqlServerTest]
    class SqlServerSystemDataClientAdaptTransportConnectionTests : AdaptTransportConnectionTests
    {
        public SqlServerSystemDataClientAdaptTransportConnectionTests() : base(BuildSqlDialect.MsSqlServer)
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x =>
            {
                var connection = MsSqlSystemDataClientConnectionBuilder.Build();
                connection.Open();
                return connection;
            };
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

            Assert.That(wasAdapted, Is.True);
            Assert.That(connection, Is.Not.Null);
            Assert.That(tx, Is.Not.Null);
            Assert.That(ownsTransaction, Is.False);
        }
    }
}