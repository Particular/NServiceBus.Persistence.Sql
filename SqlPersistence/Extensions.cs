using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

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

    public static Task ExecuteNonQueryEx(this SqlCommand command)
    {
        return ExecuteNonQueryEx(command, CancellationToken.None);
    }

    public static async Task ExecuteNonQueryEx(this SqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            var message = $"Failed to ExecuteNonQuery. CommandText:{Environment.NewLine}{command.CommandText}";
            throw new Exception(message, exception);
        }
    }
}