using System;
using Microsoft.Data.SqlClient;

public static class MsSqlMicrosoftDataClientConnectionBuilder
{
    const string ConnectionString = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";

    public static SqlConnection Build()
    {
        return new SqlConnection(GetConnectionString());
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

            using (var conn = MsSqlSystemDataClientConnectionBuilder.Build())
            {
                conn.Open();
                conn.ExecuteCommand($"if not exists (select * from sysdatabases where name = '{dbName}') create database {dbName};");
            }
        }

        public static void TearDown(string tenantId)
        {
            var dbName = "nservicebus_" + tenantId.ToLower();
            DropDatabase(dbName);
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

    public static void DropDbIfCollationIncorrect()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString());
        var databaseName = connectionStringBuilder.InitialCatalog;

        connectionStringBuilder.InitialCatalog = "master";

        using (var connection = new SqlConnection(connectionStringBuilder.ToString()))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM sys.databases WHERE name = '{databaseName}' AND COALESCE(collation_name, '') <> 'SQL_Latin1_General_CP1_CS_AS'";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows) // The database collation is wront, drop so that it will be recreated
                    {
                        DropDatabase(databaseName);
                    }
                }
            }
        }
    }

    public static void CreateDbIfNotExists()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString());
        var databaseName = connectionStringBuilder.InitialCatalog;

        connectionStringBuilder.InitialCatalog = "master";

        using (var connection = new SqlConnection(connectionStringBuilder.ToString()))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"select * from master.dbo.sysdatabases where name='{databaseName}'";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows) // exists
                    {
                        return;
                    }
                }

                command.CommandText = $"CREATE DATABASE {databaseName} COLLATE SQL_Latin1_General_CP1_CS_AS";
                command.ExecuteNonQuery();
            }
        }
    }

    static string GetConnectionString()
    {
        var connection = Environment.GetEnvironmentVariable("SQLServerConnectionString");

        if (string.IsNullOrWhiteSpace(connection))
        {
            return ConnectionString;
        }

        return connection;
    }

    static void DropDatabase(string databaseName)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString())
        {
            InitialCatalog = "master"
        };

        using (var connection = new SqlConnection(connectionStringBuilder.ToString()))
        {
            connection.Open();
            using (var dropCommand = connection.CreateCommand())
            {
                dropCommand.CommandText = $"use master; if exists(select * from sysdatabases where name = '{databaseName}') begin alter database {databaseName} set SINGLE_USER with rollback immediate; drop database {databaseName}; end; ";
                dropCommand.ExecuteNonQuery();
            }
        }
    }
}
