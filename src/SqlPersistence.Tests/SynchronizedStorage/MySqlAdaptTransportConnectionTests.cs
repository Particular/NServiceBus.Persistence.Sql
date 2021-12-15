using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;

[MySqlOnly]
class MySqlAdaptTransportConnectionTests : AdaptTransportConnectionTests
{
    public MySqlAdaptTransportConnectionTests() : base(BuildSqlDialect.MySql)
    {
    }

    protected override bool SupportsDistributedTransactions => false;

    protected override Func<string, DbConnection> GetConnection()
    {
        return x =>
        {
            var connection = MySqlConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }
}