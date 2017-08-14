using NServiceBus.Persistence.Sql;

namespace NServiceBus
{
    using System;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Obsolete: Use 'persistence.UseSchema&lt;SqlDialect.DialectType&gt;()' instead. Will be removed in version 4.0.0.
        /// </summary>
        [Obsolete("Use 'persistence.UseSchema<SqlDialect.DialectType>()' instead. Will be removed in version 4.0.0.", true)]
        public static void SqlVariant(this PersistenceExtensions<SqlPersistence> configuration, SqlVariant sqlVariant)
        {
            throw new NotImplementedException();
        }
    }
}