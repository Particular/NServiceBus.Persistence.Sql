namespace NServiceBus
{
    using System.Data.Common;

    public abstract partial class SqlDialect
    {
        /// <summary>
        /// PostgreSql
        /// </summary>
        public partial class PostgreSql : SqlDialect
        {
            internal override void FillParameter(DbParameter parameter, string paramName, object value)
            {
                parameter.ParameterName = paramName;
                parameter.Value = value;
            }

            internal override CommandWrapper CreateCommand(DbConnection connection)
            {
                var command = connection.CreateCommand();
                return new CommandWrapper(command, this);
            }
        }
    }
}