using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;

[TestFixture]
public class OracleOutboxPersisterTests : OutboxPersisterTests
{
    public OracleOutboxPersisterTests() : base(BuildSqlDialect.Oracle, "Particular2")
    {
    }

    protected override bool SupportsSchemas() => true;

    protected override Func<string, DbConnection> GetConnection()
    {
        return schema =>
        {
            var key = schema == null
                ? "OracleConnectionString"
                : $"OracleConnectionString_{schema}";

            var connection = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new Exception($"The tests require a connection string to be configured for the custom schema '{schema}'. The connection string for that schema needs to be added as '{key}' environment variable.");
            }
            return new OracleConnection(connection);
        };
    }

    protected override string GetTablePrefix()
    {
        return "OUTBOX PERSISTER";
    }

    protected override string GetTableSuffix()
    {
        return "_OD";
    }
}