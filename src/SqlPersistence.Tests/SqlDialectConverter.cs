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
                var sqlServer = new SqlDialect.MsSqlServer();
                if (schema != null)
                {
                    sqlServer.Schema = schema;
                }
                return sqlServer;
            case BuildSqlDialect.MySql:
                return new SqlDialect.MySql();
            case BuildSqlDialect.PostgreSql:
                var postgreSql = new SqlDialect.PostgreSql
                {
                    JsonBParameterModifier = parameter =>
                    {
                        var npgsqlParameter = (Npgsql.NpgsqlParameter)parameter;
                        npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                    }
                };
                if (schema != null)
                {
                    postgreSql.Schema = schema;
                }
                return postgreSql;
            case BuildSqlDialect.Oracle:
                var oracle = new SqlDialect.Oracle();
                return oracle;
            default:
                throw new Exception($"Unknown BuildSqlDialect: {sqlDialect}.");
        }
    }
}