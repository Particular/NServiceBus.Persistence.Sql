namespace NServiceBus
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    using System.Text;

    public abstract partial class SqlDialect
    {
        /// <summary>
        /// Oracle
        /// </summary>
        public partial class Oracle : SqlDialect
        {
            volatile PropertyInfo parametersByNameProperty;

            internal override void SetJsonParameterValue(DbParameter parameter, object value)
            {
                SetParameterValue(parameter, value);
            }

            internal override void SetParameterValue(DbParameter parameter, object value)
            {
                if (value is Guid)
                {
                    parameter.Value = value.ToString();
                    return;
                }

                if (value is Version)
                {
                    parameter.DbType = DbType.String;
                    parameter.Value = value.ToString();
                    return;
                }

                parameter.Value = value;
            }

            internal override CommandWrapper CreateCommand(DbConnection connection)
            {
                var command = connection.CreateCommand();

                if (parametersByNameProperty == null)
                {
                    var type = command.GetType();
                    parametersByNameProperty = type.GetProperty("BindByName") ??
                                               // someone might be using DevArt Oracle provider which uses PassParametersByName
                                               type.GetProperty("PassParametersByName");
                    if (parametersByNameProperty == null)
                    {
                        throw new Exception($"Could not extract property 'BindByName' nor 'PassParametersByName' from '{type.AssemblyQualifiedName}'. The property is required to make sure the parameters passed to the commands can be passed by name and do not depend on the order they are added to the command.");
                    }
                }

                parametersByNameProperty.SetValue(command, true);

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

            internal override CommandBehavior ModifyBehavior(DbConnection connection, CommandBehavior baseBehavior)
            {
                return baseBehavior;
            }

            internal override object GetCustomDialectDiagnosticsInfo()
            {
                return new { CustomSchema = string.IsNullOrEmpty(Schema) };
            }

            internal string Schema { get; set; }

            string SchemaPrefix => Schema != null ? $"\"{Schema.ToUpper()}\"." : "";
        }
    }
}