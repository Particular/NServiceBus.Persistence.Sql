using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SqlPersistenceSettingsAttribute : Attribute
    {
        public bool MsSqlServerScripts { get; }
        public bool MySqlScripts { get; }
        public string OutputPath { get; }

        public SqlPersistenceSettingsAttribute(
            bool msSqlServerScripts = false,
            bool mySqlScripts = false,
            string outputPath = ""
            )
        {
            MySqlScripts = mySqlScripts;
            MsSqlServerScripts = msSqlServerScripts;
            OutputPath = outputPath;
        }
    }
}