using System;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;

class Program
{
    static Task Main()
    {
        return EndpointStarter.Start("SqlPersistence.Sample.PostgreSql",
            persistence =>
            {
                var sqlDialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
                sqlDialect.JsonBParameterModifier(parameter =>
                {
                    var npgsqlParameter = (NpgsqlParameter)parameter;
                    npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                });
                persistence.TablePrefix("PostgreSql");
                var connection = Environment.GetEnvironmentVariable("PostgreSqlConnectionString");
                if (string.IsNullOrWhiteSpace(connection))
                {
                    throw new Exception("PostgreSqlConnectionString environment variable is empty");
                }
                persistence.ConnectionBuilder(() => new NpgsqlConnection(connection));
            });
    }
}