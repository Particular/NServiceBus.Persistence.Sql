using System;
using Microsoft.Data.SqlClient;

public static class MsSqlMicrosoftDataClientConnectionBuilder
{
    public static SqlConnection Build() => new(GetConnectionString());

    public static bool IsSql2016OrHigher()
    {
        using var connection = Build();
        connection.Open();
        return Version.Parse(connection.ServerVersion).Major >= 13;
    }

    public static class MultiTenant
    {
        const string DefaultDatabaseName = "nservicebus";

        public static void Setup(string tenantId)
        {
            var dbName = "nservicebus_" + tenantId.ToLower();

            var sqlConnection = MsSqlMicrosoftDataClientConnectionBuilder.Build();
            using var conn = sqlConnection;
            conn.Open();
            conn.ExecuteCommand($"if not exists (select * from sysdatabases where name = '{dbName}') create database {dbName};");
        }

        public static void TearDown(string tenantId)
        {
            var dbName = "nservicebus_" + tenantId.ToLower();
            DropDatabase(dbName);
        }

        public static SqlConnection Build(string tenantId)
        {
            var connectionBuilder = GetBaseConnectionString();

            bool foundDatabaseValue = connectionBuilder.TryGetValue("Database", out _);
            if (foundDatabaseValue)
            {
                connectionBuilder["Database"] = $"nservicebus_{tenantId.ToLower()}";
            }
            else if (!string.IsNullOrEmpty(connectionBuilder.InitialCatalog))
            {
                connectionBuilder.InitialCatalog = $"nservicebus_{tenantId.ToLower()}";
            }

            return new SqlConnection(connectionBuilder.ToString());
        }

        static SqlConnectionStringBuilder GetBaseConnectionString()
        {
            var connection = Environment.GetEnvironmentVariable("SQLServerConnectionString");
            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new Exception("SQLServerConnectionString environment variable is empty");
            }

            var connectionStringBuilder = new SqlConnectionStringBuilder(connection);
            bool foundDatabaseValue = connectionStringBuilder.TryGetValue("Database", out var databaseValue);
            if (connectionStringBuilder.InitialCatalog != DefaultDatabaseName || (foundDatabaseValue && databaseValue.ToString() != DefaultDatabaseName))
            {
                throw new Exception($"Environment variable `SQLServerConnectionString` must include a connection string that specifies a database name of `{DefaultDatabaseName}` to test multi-tenant operations.");
            }

            return connectionStringBuilder;
        }
    }

    public static void DropDbIfCollationIncorrect()
    {
        string connectionString = GetConnectionString();

        var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = connectionStringBuilder.InitialCatalog;

        connectionStringBuilder.InitialCatalog = "master";

        using var connection = new SqlConnection(connectionStringBuilder.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM sys.databases WHERE name = '{databaseName}' AND COALESCE(collation_name, '') <> 'SQL_Latin1_General_CP1_CS_AS'";
        using var reader = command.ExecuteReader();
        if (reader.HasRows) // The database collation is wront, drop so that it will be recreated
        {
            DropDatabase(databaseName);
        }
    }

    public static void CreateDbIfNotExists()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString());
        var databaseName = connectionStringBuilder.InitialCatalog;

        connectionStringBuilder.InitialCatalog = "master";

        using var connection = new SqlConnection(connectionStringBuilder.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
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

    static string GetConnectionString()
    {
        var connection = Environment.GetEnvironmentVariable("SQLServerConnectionString");

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new Exception("SQLServerConnectionString environment variable is empty");
        }

        return connection;
    }

    static void DropDatabase(string databaseName)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString()) { InitialCatalog = "master" };

        using var connection = new SqlConnection(connectionStringBuilder.ToString());
        connection.Open();

        using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"use master; if exists(select * from sysdatabases where name = '{databaseName}') begin alter database {databaseName} set SINGLE_USER with rollback immediate; drop database {databaseName}; end; ";
        dropCommand.ExecuteNonQuery();
    }

    public static void EnableSnapshotIsolation()
    {
        using var connection = new SqlConnection(GetConnectionString());

        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE {connection.Database} SET ALLOW_SNAPSHOT_ISOLATION ON";
        _ = command.ExecuteNonQuery();
    }
}