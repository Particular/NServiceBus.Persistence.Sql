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
        /// False to skip Saga script production.
        /// Defaults to True.
        /// </summary>
        public bool ProduceSagaScripts { get; set; } = true;

        /// <summary>
        /// False to skip Timeout script production.
        /// Defaults to True.
        /// </summary>
        public bool ProduceTimeoutScripts { get; set; } = true;

        /// <summary>
        /// False to skip Subscription script production.
        /// Defaults to True.
        /// </summary>
        public bool ProduceSubscriptionScripts { get; set; } = true;

        /// <summary>
        /// False to skip Outbox script production.
        /// Defaults to True.
        /// </summary>
        public bool ProduceOutboxScripts { get; set; } = true;

        /// <summary>
        /// Path to promote SQL installation scripts to.
        /// The token '$(SolutionDir)' will be replace with the current solution directory.
        /// The token '$(ProjectDir)' will be replace with the current project directory.
        /// The path calculation is performed relative to the current project directory.
        /// </summary>
        public string ScriptPromotionPath { get; set; }
    }
}