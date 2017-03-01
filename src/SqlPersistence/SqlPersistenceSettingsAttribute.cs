using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SqlPersistenceSettingsAttribute : Attribute
    {
        public bool MsSqlServerScripts { get; }
        public bool MySqlScripts { get; }
        public string ScriptPromotionPath { get; }

        public SqlPersistenceSettingsAttribute(
            bool msSqlServerScripts = false,
            bool mySqlScripts = false,
            string scriptPromotionPath = null
            )
        {
            MySqlScripts = mySqlScripts;
            MsSqlServerScripts = msSqlServerScripts;
            ScriptPromotionPath = scriptPromotionPath;
        }
    }
}