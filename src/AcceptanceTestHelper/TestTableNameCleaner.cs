public class TestTableNameCleaner
{
    public static string Clean(string endpointName, int maxLength = int.MaxValue)
    {
        var tablePrefix = TableNameCleaner.Clean(endpointName);

        if (tablePrefix.Length > maxLength)
        {
            //We need to make sure that tableprefix is deterministic and unique per endpoint to prevent sharing db tables between endpoints 
            tablePrefix = tablePrefix.Substring(0, maxLength - 8) + endpointName.GetHashCode().ToString("X8");
        }

        return tablePrefix;
    }
}
