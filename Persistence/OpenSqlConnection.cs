using System.Data.SqlClient;

class OpenSqlConnection
{
    public static SqlConnection New(string connectionString)
    {
        var sqlConnection = new SqlConnection(connectionString);
        sqlConnection .Open();
        return sqlConnection;
    }
}