namespace TestHelper
{
    using Npgsql;
    using System;

    public class AuroraPostgreSqlConnectionBuilder
    {
        public static NpgsqlConnection Build()
        {
            var connection = Environment.GetEnvironmentVariable("AuroraPostgreSqlConnectionString");
            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new Exception("AuroraPostgreSqlConnectionString environment variable is empty");
            }
            return new NpgsqlConnection(connection);
        }
    }
}