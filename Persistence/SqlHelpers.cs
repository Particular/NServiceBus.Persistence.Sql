using System.Data.SqlClient;

class SqlHelpers
{
    public static SqlConnection New(string connectionString)
    {
        var sqlConnection = new SqlConnection(connectionString);
        sqlConnection .Open();
        return sqlConnection;
    }

    internal static void Execute(string connectionString,string script)
    {
        using (var sqlConnection = New(connectionString))
        using (var command = new SqlCommand(script, sqlConnection))
        {
            command.ExecuteNonQuery();
        }
    }
}