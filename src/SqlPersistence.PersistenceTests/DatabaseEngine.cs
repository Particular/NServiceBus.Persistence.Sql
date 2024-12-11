namespace NServiceBus.PersistenceTesting;

using Persistence.Sql.ScriptBuilder;

public record DatabaseEngine(SqlDialect SqlDialect, BuildSqlDialect BuildSqlDialect, bool SupportsDtc)
{
    public static DatabaseEngine MsSqlServer => new(new SqlDialect.MsSqlServer(), BuildSqlDialect.MsSqlServer, true);
    public static DatabaseEngine Postgres => new(new SqlDialect.PostgreSql(), BuildSqlDialect.PostgreSql, false);
    public static DatabaseEngine MySql => new(new SqlDialect.MySql(), BuildSqlDialect.MySql, false);
    public static DatabaseEngine Oracle => new(new SqlDialect.Oracle(), BuildSqlDialect.Oracle, false);

    public override string ToString() => BuildSqlDialect.ToString();
}