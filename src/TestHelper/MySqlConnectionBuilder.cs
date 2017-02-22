using System;
using MySql.Data.MySqlClient;

public static class MySqlConnectionBuilder
{
    public static MySqlConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable("MySQLConnectionString");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new Exception("MySQLConnectionString environment variable is empty");
        }
        return new MySqlConnection(connection);
    }
}