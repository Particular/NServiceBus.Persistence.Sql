using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

static class SqlHelpers
{

    internal static async Task ExecuteTableCommand(this DbConnection connection, IEnumerable<string> scripts, string tablePrefix)
    {
        foreach (var script in scripts)
        {
            await connection.ExecuteTableCommand(script, tablePrefix);
        }
    }

    internal static async Task ExecuteTableCommand(this DbConnection connection, string script, string tablePrefix)
    {
        //TODO: catch   DbException   "Parameter XXX must be defined" for mysql
        // throw and hint to add 'Allow User Variables=True' to connection string
        using (var command = connection.CreateCommand())
        {
            command.CommandText = script;
            command.AddParameter("tablePrefix", tablePrefix);
            await command.ExecuteNonQueryEx();
        }
    }
}