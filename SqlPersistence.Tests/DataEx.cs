using System.Data.Common;

public static class DataEx
{
    public static void ExecuteCommand(this DbConnection connection, string script, string endpointName)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = script;
            command.AddParameter("tablePrefix", $"{endpointName}_");
            command.ExecuteNonQuery();
        }
    }
}