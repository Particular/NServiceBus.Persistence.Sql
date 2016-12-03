using System.Data.SqlClient;
using System.Threading.Tasks;
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
        return EndpointStarter.Start("SqlPersistence.Sample.MsSqlServer", persistence =>
        {
            persistence.SqlVarient(SqlVarient.MsSqlServer);
            persistence.ConnectionBuilder(async () =>
            {
                var connection = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencesample;Integrated Security=True");
                await connection.OpenAsync();
                return connection;
            });
        });
    }
}