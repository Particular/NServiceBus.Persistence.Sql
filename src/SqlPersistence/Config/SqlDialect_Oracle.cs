namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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
            volatile PropertyInfo bindByName;

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

                if (bindByName == null)
                {
                    var type = command.GetType();
                    bindByName = type.GetProperty("BindByName");
                    if (bindByName == null)
                    {
                        throw new Exception($"Could not extract field 'BindByName' from '{type.AssemblyQualifiedName}'.");
                    }
                }
                bindByName.SetValue(command, true);

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

            internal override void AddExtraDiagnosticsInfo(Dictionary<string, object> diagnostics)
            {
                diagnostics.Add("CustomSchema", string.IsNullOrEmpty(Schema));
            }

            internal string Schema { get; set; }

            string SchemaPrefix => Schema != null ? $"\"{Schema.ToUpper()}\"." : "";
        }
    }
}