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

            internal override void SetJsonParameterValue(DbParameter parameter, object value)
            {
                SetParameterValue(parameter, value);
            }

            internal override void SetParameterValue(DbParameter parameter, object value)
            {
                //TODO: do ArraySegment fro outbox
                if (value is ArraySegment<char> charSegment)
                {
                    if (parameter is SqlParameter sqlParameter) {
                        sqlParameter.Value = charSegment.Array;
                        sqlParameter.Offset = charSegment.Offset;
                        sqlParameter.Size = charSegment.Count;
                    }
                    #if NETSTANDARD2_0
                    else if (parameter is Microsoft.Data.SqlClient.SqlParameter msSqlParameter) {
                        msSqlParameter.Value = charSegment.Array;
                        msSqlParameter.Offset = charSegment.Offset;
                        msSqlParameter.Size = charSegment.Count;
                    }
                    #endif
                }
                else
                {
                    parameter.Value = value;
                }
            }

            internal override CommandWrapper CreateCommand(DbConnection connection)
            {
                var command = connection.CreateCommand();
                return new CommandWrapper(command, this);
            }

            internal override object GetCustomDialectDiagnosticsInfo()
            {
                return new
                {
                    CustomSchema = string.IsNullOrEmpty(Schema),
                    DoNotUseTransportConnection
                };
            }

            internal string Schema { get; set; }
        }
    }
}