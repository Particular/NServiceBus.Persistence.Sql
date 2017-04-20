using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

static class SqlHelpers
{

    public static async Task ExecuteTableCommand(this DbConnection connection, DbTransaction transaction, IEnumerable<string> scripts, string tablePrefix, string schema)
    {
        foreach (var script in scripts)
        {
            await connection.ExecuteTableCommand(transaction, script, tablePrefix, schema);
        }
    }

    public static async Task ExecuteTableCommand(this DbConnection connection, DbTransaction transaction, IEnumerable<string> scripts)
    {
        foreach (var script in scripts)
        {
            await connection.ExecuteTableCommand(transaction, script);
        }
    }

    public static async Task ExecuteTableCommand(this DbConnection connection, DbTransaction transaction, string script, string tablePrefix, string schema)
    {
        //TODO: catch   DbException   "Parameter XXX must be defined" for mysql
        // throw and hint to add 'Allow User Variables=True' to connection string
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = script;
            command.AddParameter("tablePrefix", tablePrefix);
            command.AddParameter("schema", schema);
            await command.ExecuteNonQueryEx();
        }
    }

    public static async Task ExecuteTableCommand(this DbConnection connection, DbTransaction transaction, string script, string tablePrefix)
    {
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = script;
            command.AddParameter("tablePrefix", tablePrefix);
            await command.ExecuteNonQueryEx();
        }
    }

    public static async Task ExecuteTableCommand(this DbConnection connection, DbTransaction transaction, string script)
    {
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = script;
            await command.ExecuteNonQueryEx();
        }
    }
}