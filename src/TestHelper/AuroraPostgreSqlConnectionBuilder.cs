using Npgsql;
using System;

public class AuroraPostgreSqlConnectionBuilder
{
    public const string EnvVarName = "AuroraPostgreSqlConnectionString";

    public static NpgsqlConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable(EnvVarName);
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new Exception($"{EnvVarName} environment variable is empty");
        }
        return new NpgsqlConnection(connection);
    }
}