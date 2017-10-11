namespace NServiceBus
{
    using System;
    using System.Data.Common;

    public abstract partial class SqlDialect
    {
        /// <summary>
        /// PostgreSql
        /// </summary>
        public partial class PostgreSql : SqlDialect
        {
            internal override void SetJsonParameterValue(DbParameter parameter, object value)
            {
                JsonBParameterModifier(parameter);
                SetParameterValue(parameter, value);
            }

            internal override void SetParameterValue(DbParameter parameter, object value)
            {
                parameter.Value = value;
            }

            internal override CommandWrapper CreateCommand(DbConnection connection)
            {
                var command = connection.CreateCommand();
                return new CommandWrapper(command, this);
            }

            internal Action<DbParameter> JsonBParameterModifier { get; set; }
        }
    }
}