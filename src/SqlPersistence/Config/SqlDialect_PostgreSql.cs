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
                ValidateJsonBModifier();
                JsonBParameterModifier(parameter);
                SetParameterValue(parameter, value);
            }

            void ValidateJsonBModifier()
            {
                if (JsonBParameterModifier != null)
                {
                    return;
                }

                var parameterProp = "NpgsqlParameter.NpgsqlDbType";
                var jsonb = "NpgsqlDbType.Jsonb";
                var parameterModifier = $"{nameof(SqlDialectSettings)}<{nameof(PostgreSql)}>.{nameof(SqlPersistenceConfig.JsonBParameterModifier)}()";
                var error = $@"The {parameterModifier} method has not been set.
Npgsql requires that parameters that pass JSONB data explicitly have {parameterProp} set to {jsonb}.
Npgsql does not infer this based on the column type.
It is not possible for the Sql Persistence to control this setting while still avoiding a reference to Npgsql.
As such it is necessary to explicitly set {parameterProp} to {jsonb} via a call to {parameterModifier}:

var dialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
dialect.JsonBParameterModifier(parameter =>
{{
    var npgsqlParameter = (NpgsqlParameter)parameter;
    npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
}});";
                throw new Exception(error);
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
                if (tablePrefix.Length <= 20)
                {
                    return;
                }
                throw new Exception($"Table prefix '{tablePrefix}' contains more than 20 characters, which is not supported by SQL persistence using PostgreSQL. Shorten the endpoint name or specify a custom tablePrefix using endpointConfiguration.{nameof(SqlPersistenceConfig.TablePrefix)}(tablePrefix).");
            }
        }
    }
}