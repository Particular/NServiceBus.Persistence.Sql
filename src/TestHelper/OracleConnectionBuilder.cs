#if NETFRAMEWORK
using System;
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
#endif