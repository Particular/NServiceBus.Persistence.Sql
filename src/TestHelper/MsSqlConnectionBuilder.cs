using System;
using System.Data.SqlClient;

public static class MsSqlConnectionBuilder
{
    const string ConnectionString = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";

    public static SqlConnection Build()
    {
        var connection = Environment.GetEnvironmentVariable("SQLServerConnectionString");
        if (string.IsNullOrWhiteSpace(connection))
        {
            return new SqlConnection(ConnectionString);
        }
        return new SqlConnection(connection);
    }

    public static bool IsSql2016OrHigher()
    {
        using (var connection = Build())
        {
            connection.Open();
            return Version.Parse(connection.ServerVersion).Major >= 13;
        }
    }

    public static class MultiTenant
    {
        public static void Setup(string tenantId)
        {
            var dbName = "nservicebus_" + tenantId.ToLower();

            using (var conn = MsSqlConnectionBuilder.Build())
            {
                conn.Open();
                conn.ExecuteCommand($"if not exists (select * from sysdatabases where name = '{dbName}') create database {dbName};");
            }
        }

        public static void TearDown(string tenantId)
        {
            var dbName = "nservicebus_" + tenantId.ToLower();

            using (var conn = MsSqlConnectionBuilder.Build())
            {
                conn.Open();
                conn.ExecuteCommand($"use master; if exists (select * from sysdatabases where name = '{dbName}') begin alter database {dbName} set SINGLE_USER with rollback immediate; drop database {dbName}; end;");
            }
        }

        public static SqlConnection Build(string tenantId)
        {
            var connection = GetBaseConnectionString().Replace(";Database=nservicebus;", $";Database=nservicebus_{tenantId.ToLower()};");
            return new SqlConnection(connection);
        }

        static string GetBaseConnectionString()
        {
            var connection = Environment.GetEnvironmentVariable("SQLServerConnectionString");
            if (string.IsNullOrWhiteSpace(connection))
            {
                connection = ConnectionString;
            }

            if (!connection.Contains(";Database=nservicebus;"))
            {
                throw new Exception("Environment variable `SQLServerConnectionString` must include a connection string that specifies a database name of `nservicebus` to test multi-tenant operations.");
            }

            return connection;
        }
    }


}