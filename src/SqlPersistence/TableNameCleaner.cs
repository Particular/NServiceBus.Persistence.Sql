static class TableNameCleaner
{
    public static string Clean(string value)
    {
        return value.Replace('.', '_');
    }
}