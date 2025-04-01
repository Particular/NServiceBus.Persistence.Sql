using System;
using Npgsql;

public static class PostgreSqlConnectionBuilder
{
    static readonly NpgsqlDataSource _dataSource;

    static PostgreSqlConnectionBuilder()
    {
        var connectionString = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _dataSource = NpgsqlDataSource.Create(connectionString);
        }
    }

    public static NpgsqlConnection Build()
    {
        if (_dataSource == null)
        {
            throw new Exception("PostgreSqlConnectionString environment variable is empty");
        }

        return _dataSource.CreateConnection();
    }
}