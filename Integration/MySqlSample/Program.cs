using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NServiceBus;
using NServiceBus.Persistence.Sql;

class Program
{
    static void Main()
    {
        Start().GetAwaiter().GetResult();
    }

    static Task Start()
    {
        return EndpointStarter.Start("SqlPersistence.Sample.MySql", persistence =>
        {
            persistence.SqlVarient(SqlVarient.MySql);
            persistence.ConnectionBuilder(async () =>
            {
                var connectionString = "server=localhost;user=root;database=sqlpersistencesample;port=3306;password=Password1;Allow User Variables=True";
                var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            });
        });
    }
}