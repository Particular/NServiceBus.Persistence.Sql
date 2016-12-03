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

    internal static Task Execute(Func<Task<DbConnection>> connectionBuilder, string script, Action<DbCommand> manipulateCommand)
    {
        return Execute(connectionBuilder, new List<string> {script}, manipulateCommand);
    }

    internal static Task Execute(string connection, string script, Action<DbCommand> manipulateCommand)
    {
        return Execute(() => New(connection), new List<string> {script}, manipulateCommand);
    }

    internal static async Task Execute(Func<Task<DbConnection>> connectionBuilder, IEnumerable<string> scripts, Action<DbCommand> manipulateCommand)
    {
        using (var connection = await connectionBuilder())
        {
            foreach (var script in scripts)
            {
                //TODO: catch   DbException   "Parameter XXX must be defined" for mysql
                // throw and hint to add 'Allow User Variables=True' to connection string
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = script;
                    manipulateCommand(command);
                    await command.ExecuteNonQueryEx();
                }
            }
        }
    }
}