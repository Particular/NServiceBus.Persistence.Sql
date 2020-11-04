namespace SqlServerMicrosoftData
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NServiceBus.Transport;
    using NUnit.Framework;

    class SqlServerMicrosoftDataClientAdaptTransportConnectionTests : AdaptTransportConnectionTests
    {
        public SqlServerMicrosoftDataClientAdaptTransportConnectionTests() : base(BuildSqlDialect.MsSqlServer)
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x =>
            {
                var connection = MsSqlMicrosoftDataClientConnectionBuilder.Build();
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

            var result = await sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(),
                altConnectionManager,
                (conn, tx, arg3) => new StorageSession(conn, tx, false, null));

            Assert.IsNotNull(result);
        }
    }
}