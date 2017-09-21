namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Data.SqlClient;

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

            internal override void AddCreationScriptParameters(DbCommand command)
            {
                command.AddParameter("schema", Schema);
            }

            internal override void FillParameter(DbParameter parameter, string paramName, object value)
            {
                if (value is ArraySegment<char> charSegment)
                {
                    var sqlParameter = (SqlParameter)parameter;

                    sqlParameter.ParameterName = paramName;
                    sqlParameter.Value = charSegment.Array;
                    sqlParameter.Offset = charSegment.Offset;
                    sqlParameter.Size = charSegment.Count;
                }
                else
                {
                    parameter.ParameterName = paramName;
                    parameter.Value = value;
                }
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