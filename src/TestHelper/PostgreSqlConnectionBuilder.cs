using System;
using Npgsql;
using NUnit.Framework;

public static class PostgreSqlConnectionBuilder
{
    public static NpgsqlConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
        if (string.IsNullOrWhiteSpace(connection))
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                Assert.Ignore("Ignoring PostgreSQL test");
            }
            throw new Exception("PostgreSqlConnectionString environment variable is empty");
        }
        return new NpgsqlConnection(connection);
    }
}