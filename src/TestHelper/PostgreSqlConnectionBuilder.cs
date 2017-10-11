using System;
using Npgsql;

public static class PostgreSqlConnectionBuilder
{
    public static NpgsqlConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new Exception("PostgreSqlConnectionString environment variable is empty");
        }
        var npgsqlConnection = new NpgsqlConnection(connection);
        //npgsqlConnection.CreateCommand().Parameters.AddWithValue("",npg)
        return npgsqlConnection;
    }
}