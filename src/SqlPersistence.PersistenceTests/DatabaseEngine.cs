namespace NServiceBus.PersistenceTesting;

using System.Collections.Generic;
using System.Data;
using Persistence.Sql.ScriptBuilder;

public record DatabaseEngine(SqlDialect SqlDialect,
    BuildSqlDialect BuildSqlDialect,
    bool SupportsDtc,
    List<IsolationLevel> SupportedAdoIsolationLevels,
    List<System.Transactions.IsolationLevel> SupportedScopeIsolationLevels)
{
    public static DatabaseEngine MsSqlServer => new(new SqlDialect.MsSqlServer(),
        BuildSqlDialect.MsSqlServer,
        true,
        [IsolationLevel.Serializable, IsolationLevel.ReadCommitted, IsolationLevel.RepeatableRead, IsolationLevel.Snapshot],
        [System.Transactions.IsolationLevel.Serializable, System.Transactions.IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.RepeatableRead, System.Transactions.IsolationLevel.Snapshot]);

    public static DatabaseEngine Postgres => new(new SqlDialect.PostgreSql(),
        BuildSqlDialect.PostgreSql,
        false,
        [IsolationLevel.Serializable, IsolationLevel.ReadCommitted, IsolationLevel.RepeatableRead, IsolationLevel.Snapshot],
        [System.Transactions.IsolationLevel.Serializable, System.Transactions.IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.RepeatableRead, System.Transactions.IsolationLevel.Snapshot]);

    public static DatabaseEngine MySql => new(new SqlDialect.MySql(),
        BuildSqlDialect.MySql,
        false,
        [IsolationLevel.Serializable, IsolationLevel.ReadCommitted, IsolationLevel.RepeatableRead],
        [System.Transactions.IsolationLevel.Serializable, System.Transactions.IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.RepeatableRead]);

    public static DatabaseEngine Oracle => new(new SqlDialect.Oracle(),
        BuildSqlDialect.Oracle,
        false,
        [IsolationLevel.ReadCommitted], //IsolationLevel.Serializable causes test failures on Oracle
        [System.Transactions.IsolationLevel.Serializable, System.Transactions.IsolationLevel.ReadCommitted]);

    public override string ToString() => BuildSqlDialect.ToString();
}