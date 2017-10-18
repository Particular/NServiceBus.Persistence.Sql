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

            internal override void ValidateTablePrefix(string tablePrefix)
            {
                if (tablePrefix.Length > 20)
                {
                    throw new Exception($"Table prefix '{tablePrefix}' contains more than 20 characters, which is not supported by SQL persistence using PostgreSQL. Shorten the endpoint name or specify a custom tablePrefix using endpointConfiguration.{nameof(SqlPersistenceConfig.TablePrefix)}(tablePrefix).");
                }
            }
        }
    }
}