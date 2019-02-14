namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;

    class ConnectionManager
    {
        Func<IMessageHandlerContext, string> captureTenantId;
        Func<string, DbConnection> buildConnectionFromTenantData;

        public ConnectionManager(Func<IMessageHandlerContext, string> captureTenantId, Func<string, DbConnection> buildConnectionFromTenantData)
        {
            this.captureTenantId = captureTenantId;
            this.buildConnectionFromTenantData = buildConnectionFromTenantData;
        }

        public static ConnectionManager BuildSingleTenant(Func<DbConnection> connectionBuilder)
        {
            return new ConnectionManager(context => null, _ => connectionBuilder());
        }

        public DbConnection Build(IMessageHandlerContext context, out string tenantId)
        {
            tenantId = captureTenantId(context);
            return buildConnectionFromTenantData(tenantId);
        }

        public DbConnection Build(IMessageHandlerContext context)
        {
            return Build(context, out _);
        }

        public DbConnection Build(string tenantId)
        {
            return buildConnectionFromTenantData(tenantId);
        }

        public DbConnection BuildNonContextual()
        {
            return buildConnectionFromTenantData(null);
        }

        public Task<DbConnection> OpenConnection(IMessageHandlerContext context)
        {
            var connection = Build(context);
            return OpenConnection(connection);
        }

        public Task<DbConnection> OpenNonContextualConnection()
        {
            var connection = BuildNonContextual();
            return OpenConnection(connection);
        }

        public Task<DbConnection> OpenConnection(string tenantId)
        {
            var connection = Build(tenantId);
            return OpenConnection(connection);
        }

        async Task<DbConnection> OpenConnection(DbConnection connection)
        {
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }
    }
}
