using System;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class SqlVariantConverter
{
    public static BuildSqlVariant Convert(this SqlVariant sqlVariant)
    {
        switch (sqlVariant)
        {
            case SqlVariant.MsSqlServer:
                return BuildSqlVariant.MsSqlServer;
            case SqlVariant.MySql:
                return BuildSqlVariant.MySql;
            default:
                throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
        }
    }
    public static SqlVariant Convert(this BuildSqlVariant sqlVariant)
    {
        switch (sqlVariant)
        {
            case BuildSqlVariant.MsSqlServer:
                return SqlVariant.MsSqlServer;
            case BuildSqlVariant.MySql:
                return SqlVariant.MySql;
            default:
                throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
        }
    }
}