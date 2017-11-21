#pragma warning disable 1591

namespace NServiceBus
{
    using System;
    using Persistence.Sql;

    public static partial class SqlPersistenceConfig
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3.0",
            RemoveInVersion = "4.0",
            ReplacementTypeOrMember = "persistence.SqlDialect<SqlDialect.DialectType>()")]
        public static void SqlVariant(this PersistenceExtensions<SqlPersistence> configuration, SqlVariant sqlVariant)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "3.0",
            RemoveInVersion = "4.0",
            ReplacementTypeOrMember = "persistence.SqlDialect<SqlDialect.DialectType>().Schema(\"schema_name\")")]
        public static void Schema(this PersistenceExtensions<SqlPersistence> configuration, string schema)
        {
            throw new NotImplementedException();
        }
    }
}