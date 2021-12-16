namespace PostgreSql
{
    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;

    [PostgreSqlTest]
    class PostgreSqlAdaptTransportConnectionTests : AdaptTransportConnectionTests
    {
        public PostgreSqlAdaptTransportConnectionTests() : base(BuildSqlDialect.PostgreSql)
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x =>
            {
                var connection = PostgreSqlConnectionBuilder.Build();
                return connection;
            };
        }
    }
}