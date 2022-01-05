using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;

[TestFixture, OracleTest]
public class OracleTimeoutPersisterTests : TimeoutPersisterTests
{
    public OracleTimeoutPersisterTests() : base(BuildSqlDialect.Oracle, "Particular2")
    {
    }

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
                throw new Exception($"{key} environment variable is empty");
            }
            return new OracleConnection(connection);
        };
    }

    protected override string GetTablePrefix()
    {
        var name = $"Test {TestContext.CurrentContext.Test.Name}";
        if (name.Length > 24)
        {
            name = name.Substring(0, 24);
        }
        return name.ToUpper();
    }
}