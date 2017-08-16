namespace NServiceBus
{
    using System.Data.Common;

    public abstract partial class SqlDialect
    {
        /// <summary>
        /// Microsoft SQL Server
        /// </summary>
        public partial class MsSqlServer : SqlDialect
        {
            /// <summary>
            /// Microsoft SQL Server
            /// </summary>
            public MsSqlServer()
            {
                Schema = "dbo";
            }

            internal override void AddCreationScriptParametrs(DbCommand command)
            {
                command.AddParameter("schema", Schema);
            }

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

            internal string Schema { get; set; }
        }
    }
}