using System.Data.SqlClient;

static class Extensions
{
    public static void AddParameter(this SqlCommand command, string name, object value)
    {
        command.Parameters.AddWithValue(name, value);
    }

}