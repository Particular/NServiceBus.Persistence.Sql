using System;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class SqlDialectConverter
{
    public static BuildSqlDialect Convert(this SqlDialect sqlDialect)
    {
       if(sqlDialect is SqlDialect.MsSqlServer)
       {
           return BuildSqlDialect.MsSqlServer;
       }

        if (sqlDialect is SqlDialect.MySql)
        {
            return BuildSqlDialect.MySql;
        }

        if (sqlDialect is SqlDialect.Oracle)
        {
            return BuildSqlDialect.Oracle;
        }

        throw new Exception($"Unknown SqlDialect: {sqlDialect.Name}.");
    }
    public static SqlDialect Convert(this BuildSqlDialect sqlDialect, string schema = null)
    {
        SqlDialect dialect;

        switch (sqlDialect)
        {
            case BuildSqlDialect.MsSqlServer:
                dialect = new SqlDialect.MsSqlServer();
                break;
            case BuildSqlDialect.MySql:
                dialect = new SqlDialect.MySql();
                break;
            case BuildSqlDialect.Oracle:
                dialect = new SqlDialect.Oracle();
                break;
            default:
                throw new Exception($"Unknown BuildSqlDialect: {sqlDialect}.");
        }

        dialect.Schema = schema;
        return dialect;
    }
}