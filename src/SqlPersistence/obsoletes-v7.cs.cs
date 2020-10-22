namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Data.Common;

    /// <summary>
    /// qwerty
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "7", RemoveInVersion = "8")]
    public partial class TimeoutSettings
    {
        /// <summary>
        /// qwerty
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "7", RemoveInVersion = "8")]
        public void ConnectionBuilder(Func<DbConnection> connectionBuilder)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;
    using Persistence.Sql;

    [ObsoleteEx(TreatAsErrorFromVersion = "7", RemoveInVersion = "8")]
    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// qwerty
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "7", RemoveInVersion = "8")]
        public static TimeoutSettings TimeoutSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            throw new NotImplementedException();
        }
    }
}