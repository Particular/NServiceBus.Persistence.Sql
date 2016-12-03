using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SqlPersistenceSettingsAttribute : Attribute
    {
        public SqlVarient SqlVarient { get; }

        public SqlPersistenceSettingsAttribute(SqlVarient sqlVarient = SqlVarient.All)
        {
            SqlVarient = sqlVarient;
        }
    }
}