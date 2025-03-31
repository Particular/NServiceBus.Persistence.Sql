using System;
using Npgsql;

public static class PostgreSqlConnectionBuilder
{
    static NpgsqlDataSource _dataSource;
    static NpgsqlDataSource DataSource
    {
        get
        {
            if (_dataSource == null)
            {
                var connectionString = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new Exception("PostgreSqlConnectionString environment variable is empty");
                }
                _dataSource = NpgsqlDataSource.Create(connectionString);
            }

            return _dataSource;
        }
    }

    public static NpgsqlConnection Build() => DataSource.CreateConnection();
}