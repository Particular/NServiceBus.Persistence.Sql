using System;
using System.Collections.Generic;
using System.Data.SqlClient;

class SqlHelpers
{
    public static SqlConnection New(string connectionString)
    {
        var sqlConnection = new SqlConnection(connectionString);
        sqlConnection.Open();
        return sqlConnection;
    }

    internal static void Execute(string connectionString, string script, Action<SqlParameterCollection> manipulateParameters)
    {
        Execute(connectionString, new List<string> {script}, manipulateParameters);
    }

    internal static void Execute(string connectionString, IEnumerable<string> scripts, Action<SqlParameterCollection> manipulateParameters)
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
        {
            foreach (var script in scripts)
            {
                using (var command = new SqlCommand(script, sqlConnection))
                {
                    command.AddParameter("database", database);
                    manipulateParameters(command.Parameters);
                    command.ExecuteNonQueryEx();
                }
            }
        }
    }
}