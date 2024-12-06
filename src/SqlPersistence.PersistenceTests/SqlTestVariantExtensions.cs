namespace NServiceBus.PersistenceTesting;

using System;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;
using Persistence.Sql.ScriptBuilder;

static class SqlTestVariantExtensions
{
    public static DbConnection Open(this SqlTestVariant variant) =>
        variant.DatabaseEngine.BuildSqlDialect switch
        {
            BuildSqlDialect.MsSqlServer => MsSqlMicrosoftDataClientConnectionBuilder.Build(),
            BuildSqlDialect.MySql => MySqlConnectionBuilder.Build(),
            BuildSqlDialect.Oracle => OracleConnectionBuilder.Build(),
            BuildSqlDialect.PostgreSql => new Func<DbConnection>(() =>
            {
                //HINT: Test infrastructure from Core deep-copies the tests variants but
                //      skips delegates. This code solves this problem by setting a global
                //      configuration which is good-enough for the current test setup.
                var postgreSqlDialect = (SqlDialect.PostgreSql)variant.DatabaseEngine.SqlDialect;

                postgreSqlDialect.JsonBParameterModifier = parameter =>
                {
                    var npgsqlParameter = (NpgsqlParameter)parameter;
                    npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                };
                return PostgreSqlConnectionBuilder.Build();
            })(),
            _ => throw new ArgumentOutOfRangeException(
                $"{nameof(SqlTestVariant.DatabaseEngine.SqlDialect)} '{variant.DatabaseEngine.BuildSqlDialect}' is not supported yet as a test variant.")
        };
}