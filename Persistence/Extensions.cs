using System;
using System.Data.SqlClient;

static class Extensions
{
    public static void AddParameter(this SqlCommand command, string name, object value)
    {
        command.Parameters.AddWithValue(name, value);
    }

    public static void AddParameter(this SqlCommand command, string name, Version value)
    {
        command.Parameters.AddWithValue(name, value.ToString());
    }

    public static void ExecuteNonQueryEx(this SqlCommand command)
    {
        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception exception)
        {
            var message = string.Format("Failed to ExecuteNonQuery. CommandText:{0}{1}", Environment.NewLine, command.CommandText);
            throw new Exception(message, exception);
        }
    }
}