using System;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Exposes extra configuration options for <see cref="SqlSaga{TSagaData}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SqlSagaAttribute : Attribute
    {
        /// <summary>
        /// Used to transition between different properties for saga correlation.
        /// </summary>
        public string TransitionalCorrelationProperty;

        /// <summary>
        /// The name of the tabe to use when storing the current <see cref="SqlSaga{TSagaData}"/>. 
        /// Will be appended to the value specified in <see cref="SqlPersistenceConfig.TablePrefix"/>.
        /// </summary>
        public string TableSuffix;
    }
}