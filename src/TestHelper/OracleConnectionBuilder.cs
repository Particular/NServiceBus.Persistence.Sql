using System;
using Oracle.ManagedDataAccess.Client;

public static class OracleConnectionBuilder
{
    public static OracleConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable("OracleConnectionString");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new Exception("OracleConnectionString environment variable is empty");
        }
        return new OracleConnection(connection);
    }
}