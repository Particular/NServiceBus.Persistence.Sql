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
        /// Obsolete
        /// </summary>
        [ObsoleteEx(
            Message= @"This constructor is obsolete.
 * For correlationProperty override CorrelationPropertyName on the saga implementing SqlSaga<T>.
 * For transitionalCorrelationProperty use the property " + nameof(SqlSagaAttribute) + "." + nameof(TransitionalCorrelationProperty) + @"
 * For tableSuffix use the property " + nameof(SqlSagaAttribute) + "." + nameof(TableSuffix) ,
            RemoveInVersion = "3.0",
            TreatAsErrorFromVersion = "2.0")]
        public SqlSagaAttribute(
            string correlationProperty = null,
            string transitionalCorrelationProperty = null,
            string tableSuffix = null) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlSagaAttribute"/>.
        /// </summary>
        public SqlSagaAttribute()
        {
            
        }

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