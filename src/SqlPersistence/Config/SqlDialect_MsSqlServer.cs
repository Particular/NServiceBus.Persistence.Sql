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
                SetParameterValue(parameter, value, -1);
            }

            internal override void SetParameterValue(DbParameter parameter, object value, int lengthMax)
            {
                const int defaultLength = 4000;

                if (value is ArraySegment<char> charSegment)
                {
                    if (lengthMax == 0)
                    {
                        throw new ArgumentException("Value cannot be 0 for arrays", nameof(lengthMax));
                    }

                    parameter.Value = charSegment.Array;

                    // Set to 4000 or -1 to improve query execution plan reuse
                    // Must be when exceeding 4000 characters for nvarchar(max)  https://stackoverflow.com/a/973269/199551
                    parameter.Size = lengthMax == -1 && charSegment.Count <= defaultLength
                        ? defaultLength
                        : lengthMax;
                }
                else if (value is string stringValue)
                {
                    if (lengthMax == 0)
                    {
                        throw new ArgumentException("Value cannot be 0 for strings", nameof(lengthMax));
                    }

                    parameter.Value = stringValue;

                    // Set to 4000 or -1 to improve query execution plan reuse
                    // Must be when exceeding 4000 characters for nvarchar(max)  https://stackoverflow.com/a/973269/199551
                    parameter.Size = lengthMax == -1 && stringValue.Length <= defaultLength
                        ? defaultLength
                        : lengthMax;
                }
                else
                {
                    if (lengthMax != 0)
                    {
                        throw new ArgumentException("Value must be 0 when not an array or string", nameof(lengthMax));
                    }

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

            internal string Schema { get; set; }
            bool hasConnectionBeenInspectedForEncryption;
            bool isConnectionEncrypted;
        }
    }
}