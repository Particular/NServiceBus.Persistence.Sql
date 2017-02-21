using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SqlPersistenceSettingsAttribute : Attribute
    {
        public readonly bool MsSqlServerScripts;
        public readonly bool MySqlScripts;

        public SqlPersistenceSettingsAttribute(
            bool msSqlServerScripts = false,
            bool mySqlScripts = false
            )
        {
            MySqlScripts = mySqlScripts;
            MsSqlServerScripts = msSqlServerScripts;
        }
    }
}