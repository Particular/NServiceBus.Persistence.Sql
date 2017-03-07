using System;
using System.Data.SqlClient;

public static class MsSqlConnectionBuilder
{
    public const string ConnectionString = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";


    public static SqlConnection Build()
    {
        return new SqlConnection(ConnectionString);
    }
    public static bool IsSql2016OrHigher()
    {
        using (var connection = Build())
        {
            connection.Open();
            return  Version.Parse(connection.ServerVersion).Major >= 13;
        }
    }
}