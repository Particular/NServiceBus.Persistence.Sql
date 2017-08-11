using System;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class SqlVariantConverter
{
    public static BuildSqlVariant Convert(this SqlDialect sqlDialect)
    {
       if(sqlDialect is SqlDialect.MsSqlServer)
       {
           return BuildSqlVariant.MsSqlServer;
       }

        if (sqlDialect is SqlDialect.MySql)
        {
            return BuildSqlVariant.MySql;
        }

        if (sqlDialect is SqlDialect.Oracle)
        {
            return BuildSqlVariant.Oracle;
        }

        throw new Exception($"Unknown SqlDialect: {sqlDialect.Name}.");
    }
    public static SqlDialect Convert(this BuildSqlVariant sqlVariant, string schema = null)
    {
        SqlDialect dialect;

        switch (sqlVariant)
        {
            case BuildSqlVariant.MsSqlServer:
                dialect = new SqlDialect.MsSqlServer();
                break;
            case BuildSqlVariant.MySql:
                dialect = new SqlDialect.MySql();
                break;
            case BuildSqlVariant.Oracle:
                dialect = new SqlDialect.Oracle();
                break;
            default:
                throw new Exception($"Unknown BuildSqlVariant: {sqlVariant}.");
        }

        dialect.Schema = schema;
        return dialect;
    }
}