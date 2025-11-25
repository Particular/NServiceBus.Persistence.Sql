public static class TestTableNameCleaner
{
    public static string Clean(string tableName, int maxLength = int.MaxValue)
    {
        var tablePrefix = TableNameCleaner.Clean(tableName);

        if (tablePrefix.Length > maxLength)
        {
            //We need to make sure that tableprefix is deterministic and unique per endpoint to prevent sharing db tables between endpoints 
            tablePrefix = tablePrefix[..(maxLength - 8)] + tableName.GetHashCode().ToString("X8");
        }

        return tablePrefix;
    }
}
