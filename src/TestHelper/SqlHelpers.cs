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

    // The MySQL saga script drops and recreates the database-wide sqlpersistence_raiseerror procedure, so scripts from concurrent test processes must not interleave.
    static IDisposable AcquireScriptLock(DbConnection connection)
    {
        if (connection is not MySql.Data.MySqlClient.MySqlConnection)
        {
            return null;
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"select get_lock('{ScriptLockName}', 120)";
        if (Convert.ToInt32(command.ExecuteScalar()) != 1)
        {
            throw new Exception("Could not acquire the MySQL script lock within 120 seconds.");
        }

        return new ScriptLock(connection);
    }

    sealed class ScriptLock(DbConnection connection) : IDisposable
    {
        public void Dispose()
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"select release_lock('{ScriptLockName}')";
            command.ExecuteScalar();
        }
    }

    const string ScriptLockName = "sqlpersistence_test_scripts";

    static void AddParameter(this DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}