using System;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Configuration options that are evaluated at compile time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class SqlPersistenceSettingsAttribute : Attribute
    {
        /// <summary>
        /// True to produce SQL installation scripts that target Microsoft SQL Server.
        /// Defaults to False.
        /// </summary>
        public bool MsSqlServerScripts { get; set; }

        /// <summary>
        /// True to produce SQL installation scripts that target MySql.
        /// Defaults to False.
        /// </summary>
        public bool MySqlScripts { get; set; }

        /// <summary>
        /// True to produce SQL installation scripts that target Oracle.
        /// Defaults to False.
        /// </summary>
        public bool OracleScripts { get; set; }

        /// <summary>
        /// Path to promote SQL installation scripts to.
        /// The token '$(SolutionDir)' will be replace witht he current solution directory.
        /// The token '$(ProjectDir)' will be replace witht he current solution directory.
        /// The path calculation is performed relative to the current project directory.
        /// </summary>
        public string ScriptPromotionPath { get; set; }
    }
}