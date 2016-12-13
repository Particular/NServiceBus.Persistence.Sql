static class TableNameCleaner
{
    public static object Clean(string value)
    {
        return value.Replace('.', '_');
    }
}