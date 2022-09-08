using System;
using System.Data.SqlClient;

public static class MsSqlSystemDataClientConnectionBuilder
{
    public static SqlConnection Build()
    {
        return new SqlConnection(GetConnectionString());
    }

    public static SqlConnection BuildWithoutCertificateCheck()
    {
        return new SqlConnection(GetConnectionString(true));
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
        const string DefaultDatabaseName = "nservicebus";

        public static void Setup(string tenantId, bool trustServerCertificate = false)
        {
            var dbName = "nservicebus_" + tenantId.ToLower();
            var connectionString = trustServerCertificate ? BuildWithoutCertificateCheck() : MsSqlSystemDataClientConnectionBuilder.Build();

            using (var conn = connectionString)
            {
                conn.Open();
                conn.ExecuteCommand($"if not exists (select * from sysdatabases where name = '{dbName}') create database {dbName};");
            }
        }

        public static void TearDown(string tenantId, bool trustServerCertificate = false)
        {
            var dbName = "nservicebus_" + tenantId.ToLower();
            DropDatabase(dbName, trustServerCertificate);
        }

        public static SqlConnection Build(string tenantId, bool trustServerCertificate = false)
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

            connectionBuilder.TrustServerCertificate = trustServerCertificate;

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

    public static void DropDbIfCollationIncorrect(bool trustServerCertificate = false)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString(trustServerCertificate));
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

    public static void CreateDbIfNotExists(bool trustServerCertificate = false)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString(trustServerCertificate));
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

    static string GetConnectionString(bool trustServerCertificate = false)
    {
        var connection = Environment.GetEnvironmentVariable("SQLServerConnectionString");

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new Exception("SQLServerConnectionString environment variable is empty");
        }

        if (trustServerCertificate)
        {
            connection += ";Encrypt=False";
        }

        return connection;
    }

    static void DropDatabase(string databaseName, bool trustServerCertificate = false)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString(trustServerCertificate))
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
