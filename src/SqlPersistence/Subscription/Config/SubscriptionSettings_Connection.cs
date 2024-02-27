namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Data.Common;

    public partial class SubscriptionSettings
    {
        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed.
        /// </summary>
        public void ConnectionBuilder(Func<DbConnection> connectionBuilder)
        {
            ArgumentNullException.ThrowIfNull(connectionBuilder);
            settings.Set($"SqlPersistence.ConnectionManager.{nameof(StorageType.Subscriptions)}",
                new ConnectionManager(connectionBuilder));
        }
    }
}