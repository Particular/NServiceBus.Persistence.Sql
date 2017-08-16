namespace NServiceBus
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Text;

    public abstract partial class SqlDialect
    {
        /// <summary>
        /// Oracle
        /// </summary>
        public partial class Oracle : SqlDialect
        {
            internal override void FillParameter(DbParameter parameter, string paramName, object value)
            {
                parameter.ParameterName = paramName;
                if (value is Guid)
                {
                    parameter.Value = value.ToString();
                }
                else if (value is Version)
                {
                    parameter.DbType = DbType.String;
                    parameter.Value = value.ToString();
                }
                else
                {
                    parameter.Value = value;
                }
            }

            internal override CommandWrapper CreateCommand(DbConnection connection)
            {
                var command = connection.CreateCommand();

                var bindByNameProperty = command.GetType().GetProperty("BindByName");
                bindByNameProperty.SetValue(command, true);

                return new CommandWrapper(command, this);
            }

            internal override void ValidateTablePrefix(string tablePrefix)
            {
                if (tablePrefix.Length > 25)
                {
                    throw new Exception($"Table prefix '{tablePrefix}' contains more than 25 characters, which is not supported by SQL persistence using Oracle. Shorten the endpoint name or specify a custom tablePrefix using endpointConfiguration.{nameof(SqlPersistenceConfig.TablePrefix)}(tablePrefix).");
                }
                if (Encoding.UTF8.GetBytes(tablePrefix).Length != tablePrefix.Length)
                {
                    throw new Exception($"Table prefix '{tablePrefix}' contains non-ASCII characters, which is not supported by SQL persistence using Oracle. Change the endpoint name or specify a custom tablePrefix using endpointConfiguration.{nameof(SqlPersistenceConfig.TablePrefix)}(tablePrefix).");
                }
            }
        }
    }
}