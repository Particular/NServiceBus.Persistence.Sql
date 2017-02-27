using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SqlPersistenceSettingsAttribute : Attribute
    {
        public bool MsSqlServerScripts { get; }
        public bool MySqlScripts { get; }

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