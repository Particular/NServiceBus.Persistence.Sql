using System;
using MySql.Data.MySqlClient;
using NUnit.Framework;

public static class MySqlConnectionBuilder
{
    public static MySqlConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable("MySQLConnectionString");
        if (string.IsNullOrWhiteSpace(connection))
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                Assert.Ignore("Ignoring MySQL test");
            }
            throw new Exception("MySQLConnectionString environment variable is empty");
        }
        return new MySqlConnection(connection);
    }
}