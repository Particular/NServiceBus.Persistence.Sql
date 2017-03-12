using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SqlPersistenceSettingsAttribute : Attribute
    {
        public bool MsSqlServerScripts;
        public bool MySqlScripts;
        public string ScriptPromotionPath;
    }
}