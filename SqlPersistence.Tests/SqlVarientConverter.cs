using System;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class SqlVarientConverter
{
    public static BuildSqlVarient Convert(this SqlVarient sqlVarient)
    {
        switch (sqlVarient)
        {
            case SqlVarient.MsSqlServer:
                return BuildSqlVarient.MsSqlServer;
            case SqlVarient.MySql:
                return BuildSqlVarient.MySql;
            default:
                throw new Exception($"Unknown SqlVarient: {sqlVarient}.");
        }
    }
    public static SqlVarient Convert(this BuildSqlVarient sqlVarient)
    {
        switch (sqlVarient)
        {
            case BuildSqlVarient.MsSqlServer:
                return SqlVarient.MsSqlServer;
            case BuildSqlVarient.MySql:
                return SqlVarient.MySql;
            default:
                throw new Exception($"Unknown SqlVarient: {sqlVarient}.");
        }
    }
}