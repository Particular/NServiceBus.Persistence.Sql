using NServiceBus.Persistence.Sql;

namespace NServiceBus
{
    using System;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Sets the <see cref="SqlVariant"/> to use for communicating the the current database.
        /// </summary>
        [Obsolete]
        public static void SqlVariant(this PersistenceExtensions<SqlPersistence> configuration, SqlVariant sqlVariant)
        {
        }
    }
}