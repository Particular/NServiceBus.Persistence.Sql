using System.Threading.Tasks;
using Npgsql;
using NServiceBus;

class Program
{
    static void Main()
    {
        Start().GetAwaiter().GetResult();
    }

    static Task Start()
    {
        return EndpointStarter.Start("SqlPersistence.Sample.PostgreSql", persistence =>
        {
            //TODO:
            //persistence.SqlVarient(SqlVarient.MsSqlServer);
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";
            persistence.ConnectionBuilder(async () =>
            {
                var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            });
        });
    }
}