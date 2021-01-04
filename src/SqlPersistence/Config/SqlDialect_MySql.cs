namespace NServiceBus
{
    using System.Data;
    using System.Data.Common;

    public abstract partial class SqlDialect
    {
        /// <summary>
        /// MySQL
        /// </summary>
        public partial class MySql : SqlDialect
        {
            internal override void SetJsonParameterValue(DbParameter parameter, object value)
            {
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

            internal override object GetCustomDialectDiagnosticsInfo()
            {
                return null;
            }

            internal override CommandBehavior GetBehavior(DbConnection connection)
            {
                return CommandBehavior.SingleRow | CommandBehavior.SequentialAccess;
            }
        }
    }
}