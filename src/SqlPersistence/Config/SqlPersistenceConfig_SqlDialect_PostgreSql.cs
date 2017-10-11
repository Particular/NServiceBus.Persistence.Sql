namespace NServiceBus
{
    using System;
    using System.Data.Common;
    
    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Sets a <see cref="Action"/> used to modify a <see cref="DbParameter"/> when being used for storing JsonB.
        /// </summary>
        public static void JsonBParameterModifier(this SqlDialectSettings<SqlDialect.PostgreSql> dialectSettings, Action<DbParameter> modifier)
        {
            Guard.AgainstNull(nameof(dialectSettings), dialectSettings);
            Guard.AgainstNull(nameof(modifier), modifier);
            dialectSettings.TypedDialect.JsonBParameterModifier = modifier;
        }
    }
}