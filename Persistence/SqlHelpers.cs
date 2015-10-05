using System;
using System.Data.SqlClient;

class SqlHelpers
{
    public static SqlConnection New(string connectionString)
    {
        var sqlConnection = new SqlConnection(connectionString);
        sqlConnection.Open();
        return sqlConnection;
    }

    internal static void Execute(string connectionString, string script, Action<SqlParameterCollection> manipulateParameters )
    {
        var connectionBuilder = new SqlConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var database = connectionBuilder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new Exception("Expected to have a 'InitialCatalog' in the connection string.");
        }
        using (var sqlConnection = New(connectionString))
        using (var command = new SqlCommand(script, sqlConnection))
        {
            command.AddParameter("database", database);
            manipulateParameters(command.Parameters);
            command.ExecuteNonQueryEx();
        }
    }
}