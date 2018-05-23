namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Data.Common;

    public partial class TimeoutSettings
    {
        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed.
        /// </summary>
        public void ConnectionBuilder(Func<DbConnection> connectionBuilder)
        {
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);
            settings.Set($"SqlPersistence.ConnectionBuilder.{typeof(StorageType.Timeouts).Name}", connectionBuilder);
        }
    }
}