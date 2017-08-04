using System;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class SqlVariantConverter
{
    public static BuildSqlVariant Convert(this Type sqlVariant)
    {
       if(sqlVariant == typeof(SqlDialect.MsSqlServer))
       {
           return BuildSqlVariant.MsSqlServer;
       }

        if (sqlVariant == typeof(SqlDialect.MySql))
        {
            return BuildSqlVariant.MySql;
        }

        if (sqlVariant == typeof(SqlDialect.Oracle))
        {
            return BuildSqlVariant.Oracle;
        }

        throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
    }
    public static Type Convert(this BuildSqlVariant sqlVariant)
    {
        switch (sqlVariant)
        {
            case BuildSqlVariant.MsSqlServer:
                return typeof(SqlDialect.MsSqlServer);
            case BuildSqlVariant.MySql:
                return typeof(SqlDialect.MySql);
            case BuildSqlVariant.Oracle:
                return typeof(SqlDialect.Oracle);
            default:
                throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
        }
    }
}