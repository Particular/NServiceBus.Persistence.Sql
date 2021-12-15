using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public abstract class EngineSpecificTestAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Ignoring because environment variable {0} not available.", ConnectionStringName);
        }
    }

    protected abstract string ConnectionStringName { get; }
}

public class MsSqlOnlyAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => "SQLServerConnectionString";
}

public class MySqlOnlyAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => "MySQLConnectionString";
}

public class PostgreSqlOnlyAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => "PostgreSqlConnectionString";
}

public class OracleOnlyAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => "OracleConnectionString";
}