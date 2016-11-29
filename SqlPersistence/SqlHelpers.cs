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

    internal static Task Execute(Func<Task<SqlConnection>> connectionBuilder, string script, Action<SqlParameterCollection> manipulateParameters)
    {
        return Execute(connectionBuilder, new List<string> { script }, manipulateParameters);
    }
    internal static Task Execute(string connection, string script, Action<SqlParameterCollection> manipulateParameters)
    {
        return Execute(() => New(connection), new List<string> { script }, manipulateParameters);
    }

    internal static async Task Execute(Func<Task<SqlConnection>> connectionBuilder, IEnumerable<string> scripts, Action<SqlParameterCollection> manipulateParameters)
    {
        using (var connection = await connectionBuilder())
        {
            var database = connection.Database;
            if (string.IsNullOrWhiteSpace(database))
            {
                throw new Exception("Expected to have a 'InitialCatalog' in the connection string.");
            }
            foreach (var script in scripts)
            {
                using (var command = new SqlCommand(script, connection))
                {
                    command.AddParameter("database", database);
                    manipulateParameters(command.Parameters);
                    await command.ExecuteNonQueryEx();
                }
            }
        }
    }
}