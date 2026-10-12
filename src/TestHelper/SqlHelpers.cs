using System;
using System.Data.Common;

public static class SqlHelpers
{
    public static void ExecuteCommand(this DbConnection connection, string script, Func<Exception, bool> filter = null, string schema = null)
    {
        using var scriptLock = AcquireScriptLock(connection);
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = script;
                if (command is Microsoft.Data.SqlClient.SqlCommand)
                {
                    command.AddParameter("schema", schema ?? "dbo");
                }
                command.ExecuteNonQuery();
            }
        }
        catch (Exception e) when (filter != null && filter(e))
        {
        }
    }
    public static void ExecuteCommand(this DbConnection connection, string script, string tablePrefix, Func<Exception, bool> filter = null, string schema = null)
    {
        using var scriptLock = AcquireScriptLock(connection);
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("tablePrefix", $"{tablePrefix}_");
                if (command is Microsoft.Data.SqlClient.SqlCommand)
                {
                    command.AddParameter("schema", schema ?? "dbo");
                }
                if (command is Npgsql.NpgsqlCommand)
                {
                    command.AddParameter("schema", schema ?? "public");
                }
                command.ExecuteNonQuery();
            }
        }
        catch (Exception e) when (filter != null && filter(e))
        {
        }
    }

    // MySQL scripts recreate the database-wide sqlpersistence_raiseerror procedure and PostgreSQL outbox scripts create schema-wide, unprefixed indexes,
    // so scripts from concurrent test processes must not interleave.
    static IDisposable AcquireScriptLock(DbConnection connection)
    {
        string acquire;
        string release;

        if (connection is MySql.Data.MySqlClient.MySqlConnection)
        {
            acquire = $"select get_lock('{ScriptLockName}', 120)";
            release = $"select release_lock('{ScriptLockName}')";
        }
        else if (connection is Npgsql.NpgsqlConnection)
        {
            acquire = $"select pg_advisory_lock({PostgreSqlScriptLockKey})";
            release = $"select pg_advisory_unlock({PostgreSqlScriptLockKey})";
        }
        else
        {
            return null;
        }

        using var command = connection.CreateCommand();
        command.CommandText = acquire;
        var result = command.ExecuteScalar();

        // get_lock returns 0 or null on timeout, pg_advisory_lock blocks and returns void (DBNull)
        if (connection is MySql.Data.MySqlClient.MySqlConnection && Convert.ToInt32(result) != 1)
        {
            throw new Exception("Could not acquire the MySQL script lock within 120 seconds.");
        }

        return new ScriptLock(connection, release);
    }

    sealed class ScriptLock(DbConnection connection, string release) : IDisposable
    {
        public void Dispose()
        {
            using var command = connection.CreateCommand();
            command.CommandText = release;
            command.ExecuteScalar();
        }
    }

    const string ScriptLockName = "sqlpersistence_test_scripts";
    const long PostgreSqlScriptLockKey = 7340198256;

    static void AddParameter(this DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}