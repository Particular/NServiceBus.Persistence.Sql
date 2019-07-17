using System;
using System.Data.SqlClient;

public static class MsSqlConnectionBuilder
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

    public static void DropAndCreateDb()
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(GetConnectionString());
        var databaseName = connectionStringBuilder.InitialCatalog;

        connectionStringBuilder.InitialCatalog = "master";

        using (var connection = new SqlConnection(connectionStringBuilder.ToString()))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"use master;
if exists(select * from sysdatabases where name = '{databaseName}')
begin
    alter database {databaseName} set SINGLE_USER with rollback immediate;
    drop database {databaseName};
end;

CREATE DATABASE {databaseName};";

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
}