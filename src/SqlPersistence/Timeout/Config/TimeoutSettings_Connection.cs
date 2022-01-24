namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Data.Common;

    [ObsoleteEx(
        Message = "Timeout Manager is being deprecated. See upgrade guide for guidance on migrating to transport native delayed delivery.",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public partial class TimeoutSettings
    {
        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed.
        /// </summary>
        public void ConnectionBuilder(Func<DbConnection> connectionBuilder)
        {
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);
            settings.Set($"SqlPersistence.ConnectionManager.{nameof(StorageType.Timeouts)}",
                new ConnectionManager(connectionBuilder));
        }
    }
}