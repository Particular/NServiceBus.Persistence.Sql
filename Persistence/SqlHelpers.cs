using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

class SqlHelpers
{
    public static async Task<SqlConnection> New(string connectionString)
    {
        var sqlConnection = new SqlConnection(connectionString);
        await sqlConnection.OpenAsync();
        return sqlConnection;
    }

    internal static Task Execute(string connectionString, string script, Action<SqlParameterCollection> manipulateParameters)
    {
        return Execute(connectionString, new List<string> {script}, manipulateParameters);
    }

    internal static async Task Execute(string connectionString, IEnumerable<string> scripts, Action<SqlParameterCollection> manipulateParameters)
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
        using (var sqlConnection = await New(connectionString))
        {
            foreach (var script in scripts)
            {
                using (var command = new SqlCommand(script, sqlConnection))
                {
                    command.AddParameter("database", database);
                    manipulateParameters(command.Parameters);
                    await command.ExecuteNonQueryEx();
                }
            }
        }
    }
}