using System;
using NpgsqlTypes;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class SqlDialectConverter
{
    public static BuildSqlDialect Convert(this SqlDialect sqlDialect)
    {
        if (sqlDialect is SqlDialect.MsSqlServer)
        {
            return BuildSqlDialect.MsSqlServer;
        }

        if (sqlDialect is SqlDialect.MySql)
        {
            return BuildSqlDialect.MySql;
        }

        if (sqlDialect is SqlDialect.PostgreSql)
        {
            return BuildSqlDialect.PostgreSql;
        }

        if (sqlDialect is SqlDialect.Oracle)
        {
            return BuildSqlDialect.Oracle;
        }

        throw new Exception($"Unknown SqlDialect: {sqlDialect.Name}.");
    }

    public static SqlDialect Convert(this BuildSqlDialect sqlDialect, string schema = null)
    {
        switch (sqlDialect)
        {
            case BuildSqlDialect.MsSqlServer:
                return new SqlDialect.MsSqlServer
                {
                    Schema = schema
                };
            case BuildSqlDialect.MySql:
                return new SqlDialect.MySql();
            case BuildSqlDialect.PostgreSql:
                return new SqlDialect.PostgreSql
                {
                    JsonBParameterModifier = parameter =>
                    {
                        var npgsqlParameter = (Npgsql.NpgsqlParameter)parameter;
                        npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                    }
                };
            case BuildSqlDialect.Oracle:
                return new SqlDialect.Oracle();
            default:
                throw new Exception($"Unknown BuildSqlDialect: {sqlDialect}.");
        }
    }
}