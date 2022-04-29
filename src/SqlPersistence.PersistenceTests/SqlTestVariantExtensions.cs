namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Data.Common;
    using Persistence.Sql.ScriptBuilder;

    static class SqlTestVariantExtensions
    {
        public static DbConnection Open(this SqlTestVariant variant) =>
            variant.BuildDialect switch
            {
                BuildSqlDialect.MsSqlServer => MsSqlMicrosoftDataClientConnectionBuilder.Build(),
                BuildSqlDialect.MySql => MySqlConnectionBuilder.Build(),
                BuildSqlDialect.Oracle => OracleConnectionBuilder.Build(),
                BuildSqlDialect.PostgreSql => PostgreSqlConnectionBuilder.Build(),
                _ => throw new ArgumentOutOfRangeException(
                    $"{nameof(SqlTestVariant.BuildDialect)} '{variant.BuildDialect}' is not supported yet as a test variant.")
            };
    }
}