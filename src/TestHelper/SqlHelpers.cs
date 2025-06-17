using System;
using System.Data.Common;

public static class SqlHelpers
{
    public static void ExecuteCommand(this DbConnection connection, string script, Func<Exception, bool> filter = null, string schema = null)
    {
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

    static void AddParameter(this DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}