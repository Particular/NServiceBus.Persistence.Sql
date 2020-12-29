namespace NServiceBus
{
    using System;
    using System.Data.Common;

    static class ConnectionExtensions
    {
        public static bool IsEncrypted(this SqlDialect dialect, IConnectionManager connectionManager)
        {
            if (dialect.Name != nameof(SqlDialect.MsSqlServer))
                return false;

            var connectionString = connectionManager.BuildNonContextual().ConnectionString;
            return IsConnectionEncrypted(connectionString);
        }

        public static bool IsEncrypted(this SqlDialect dialect, DbConnection dbConnection)
        {
            if (dialect.Name != nameof(SqlDialect.MsSqlServer))
                return false;

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
}