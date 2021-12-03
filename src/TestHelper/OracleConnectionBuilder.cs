using System;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;

public static class OracleConnectionBuilder
{
    public static OracleConnection Build()
    {
        return Build(false);
    }

    public static OracleConnection Build(bool disableMetadataPooling)
    {
        var connection = Environment.GetEnvironmentVariable("OracleConnectionString");

        if (string.IsNullOrWhiteSpace(connection))
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                Assert.Ignore("Ignoring Oracle test");
            }
            throw new Exception("OracleConnectionString environment variable is empty");
        }

        if (disableMetadataPooling)
        {
            var builder = new OracleConnectionStringBuilder(connection)
            {
                MetadataPooling = false
            };
            connection = builder.ConnectionString;
        }

        return new OracleConnection(connection);
    }
}
