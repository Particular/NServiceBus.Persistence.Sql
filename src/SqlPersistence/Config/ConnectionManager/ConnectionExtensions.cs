using System;
using System.Data.Common;

static class ConnectionExtensions
{
    public static bool IsEncrypted(this IConnectionManager connectionManager)
    {
        var connectionString = connectionManager.BuildNonContextual().ConnectionString;
        return IsConnectionEncrypted(connectionString);
    }

    public static bool IsEncrypted(this DbConnection dbConnection)
    {
        var connectionString = dbConnection.ConnectionString;
        return IsConnectionEncrypted(connectionString);
    }

    private static bool IsConnectionEncrypted(string connectionString)
    {
        var parser = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (parser.TryGetValue("Column Encryption Setting", out var enabled))
        {
            return ((string) enabled).Equals("enabled", StringComparison.InvariantCultureIgnoreCase);
        }

        return false;
    }
}