using System.Data.SqlClient;

public static class MsSqlConnectionBuilder
{
    public const string ConnectionString = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";


    public static SqlConnection Build()
    {
        return new SqlConnection(ConnectionString);
    }
}