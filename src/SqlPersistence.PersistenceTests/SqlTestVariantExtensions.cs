namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Data.Common;
    using Npgsql;
    using NpgsqlTypes;
    using NUnit.Framework;
    using Persistence.Sql.ScriptBuilder;

    static class SqlTestVariantExtensions
    {
        public static DbConnection Open(this SqlTestVariant variant)
        {
            return variant.BuildDialect switch
            {
                BuildSqlDialect.MsSqlServer => MsSqlMicrosoftDataClientConnectionBuilder.Build(),
                BuildSqlDialect.MySql => MySqlConnectionBuilder.Build(),
                BuildSqlDialect.Oracle => OracleConnectionBuilder.Build(),
                BuildSqlDialect.PostgreSql => new Func<DbConnection>(() =>
                {
                    //HINT: Test infrastructure from Core deep-copies the tests variants but
                    //      skips delegates. This code solves this problem by setting a global
                    //      configuration which is good-enough for the current test setup. 
                    var postgreSqlDialect = (SqlDialect.PostgreSql)variant.Dialect;

                    postgreSqlDialect.JsonBParameterModifier = parameter =>
                    {
                        var npgsqlParameter = (NpgsqlParameter)parameter;
                        npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                    };
                    return PostgreSqlConnectionBuilder.Build();
                })(),
                _ => throw new ArgumentOutOfRangeException(
                    $"{nameof(SqlTestVariant.BuildDialect)} '{variant.BuildDialect}' is not supported yet as a test variant.")
            };
        }

        public static void RequiresOutboxPessimisticConcurrencySupport(this SqlTestVariant variant)
        {
            if (!variant.UsePessimisticMode)
            {
                Assert.Ignore("Ignoring this test because it requires pessimistic concurrency support from persister.");
            }
        }
    }
}