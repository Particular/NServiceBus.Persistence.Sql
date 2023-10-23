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
                SetParameterValue(parameter, value, -1);
            }

            internal override void SetParameterValue(DbParameter parameter, object value, int lengthMax)
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

            internal override CommandBehavior ModifyBehavior(DbConnection connection, CommandBehavior baseBehavior)
            {
                return baseBehavior;
            }
        }
    }
}