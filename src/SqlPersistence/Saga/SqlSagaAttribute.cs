using System;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Exposes extra configuration options for storing <see cref="Saga{TSagaData}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SqlSagaAttribute : Attribute
    {
        /// <summary>
        /// Identifies the Saga correlation property if it cannot be determined by inspecting the ConfigureHowToFindSaga method.
        /// </summary>
        public string CorrelationProperty { get; }

        /// <summary>
        /// Used to transition between different properties for saga correlation.
        /// </summary>
        public string TransitionalCorrelationProperty { get; }

        /// <summary>
        /// The name of the table to use when storing the current <see cref="Saga{TSagaData}"/>.
        /// Will be appended to the value specified in <see cref="SqlPersistenceConfig.TablePrefix"/>.
        /// </summary>
        public string TableSuffix { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationProperty">Identifies the Saga correlation property if it cannot be determined by inspecting the ConfigureHowToFindSaga method.</param>
        /// <param name="transitionalCorrelationProperty">Used to transition between different properties for saga correlation.</param>
        /// <param name="tableSuffix">
        /// The name of the table to use when storing the current <see cref="Saga{TSagaData}"/>.
        /// Will be appended to the value specified in <see cref="SqlPersistenceConfig.TablePrefix"/>.
        /// </param>
        public SqlSagaAttribute(
            string correlationProperty = null,
            string transitionalCorrelationProperty = null,
            string tableSuffix = null)
        {
            CorrelationProperty = correlationProperty;
            TransitionalCorrelationProperty = transitionalCorrelationProperty;
            TableSuffix = tableSuffix;
        }
    }
}