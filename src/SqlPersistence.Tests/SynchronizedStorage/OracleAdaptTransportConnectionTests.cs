#if NET452
using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class OracleAdaptTransportConnectionTests : AdaptTransportConnectionTests
{
    public OracleAdaptTransportConnectionTests() : base(BuildSqlDialect.Oracle)
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x =>
        {
            var connection = OracleConnectionBuilder.Build();
            return connection;
        };
    }
}
#endif