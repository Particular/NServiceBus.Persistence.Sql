using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

class SqlHelpers
{

    public static async Task<DbConnection> New(string connectionString)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    internal static Task Execute(Func<Task<DbConnection>> connectionBuilder, string script, Action<DbCommand> manipulateParameters)
    {
        return Execute(connectionBuilder, new List<string> { script }, manipulateParameters);
    }
    internal static Task Execute(string connection, string script, Action<DbCommand> manipulateParameters)
    {
        return Execute(() => New(connection), new List<string> { script }, manipulateParameters);
    }

    internal static async Task Execute(Func<Task<DbConnection>> connectionBuilder, IEnumerable<string> scripts, Action<DbCommand> manipulateParameters)
    {
        using (var connection = await connectionBuilder())
        {
            foreach (var script in scripts)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = script;
                    manipulateParameters(command);
                    await command.ExecuteNonQueryEx();
                }
            }
        }
    }
}