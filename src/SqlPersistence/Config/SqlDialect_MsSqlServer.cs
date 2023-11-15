namespace NServiceBus
{
    using System;
    using System.Data;
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
                if (value is ArraySegment<char> charSegment)
                {
                    parameter.Value = charSegment.Array;
                    parameter.Size = charSegment.Count;
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

            internal override CommandBehavior ModifyBehavior(DbConnection connection, CommandBehavior baseBehavior)
            {
                if (!hasConnectionBeenInspectedForEncryption)
                {
                    isConnectionEncrypted = connection.IsEncrypted();
                    hasConnectionBeenInspectedForEncryption = true;
                }

                if (isConnectionEncrypted)
                {
                    baseBehavior &= ~CommandBehavior.SequentialAccess; //Remove sequential access
                }

                return baseBehavior;
            }

            internal override object GetCustomDialectDiagnosticsInfo()
            {
                return new
                {
                    CustomSchema = string.IsNullOrEmpty(Schema),
                    DoNotUseTransportConnection
                };
            }

            internal override void OptimizeForReads(DbCommand command)
            {
                foreach (DbParameter parameter in command.Parameters)
                {
                    if (parameter.Value is ArraySegment<char> charSegment)
                    {
                        // Set to 4000 or -1 to improve query execution plan reuse
                        // Must be set when exceeding 4000 characters for nvarchar(max)  https://stackoverflow.com/a/973269/199551
                        parameter.Size = charSegment.Count > 4000 ? -1 : 4000;
                    }
                    else if (parameter.Value is string stringValue)
                    {
                        // Set to 4000 or -1 to improve query execution plan reuse
                        // Must be set when exceeding 4000 characters for nvarchar(max)  https://stackoverflow.com/a/973269/199551
                        parameter.Size = stringValue.Length > 4000 ? -1 : 4000;
                    }
                }
            }

            internal string Schema { get; set; }
            bool hasConnectionBeenInspectedForEncryption;
            bool isConnectionEncrypted;
        }

    }
}