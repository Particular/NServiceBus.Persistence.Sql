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
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);
            settings.Set($"SqlPersistence.ConnectionBuilder.{typeof(StorageType.Subscriptions).Name}", connectionBuilder);
        }
    }
}