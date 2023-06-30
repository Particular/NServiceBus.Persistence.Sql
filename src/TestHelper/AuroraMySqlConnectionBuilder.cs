using MySql.Data.MySqlClient;
using System;

public class AuroraMySqlConnectionBuilder
{
    public const string EnvVarName = "AuroraMySQLConnectionString";

    public static MySqlConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable(EnvVarName);
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new Exception($"{EnvVarName} environment variable is empty");
        }
        return new MySqlConnection(connection);
    }
}