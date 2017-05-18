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
        var connection = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencesample;Integrated Security=True";
        return EndpointStarter.Start("SqlPersistence.Sample.MsSqlServer",
            persistence =>
            {
                persistence.SqlVariant(SqlVariant.MsSqlServer);
                persistence.TablePrefix("AcceptanceTests");
                persistence.ConnectionBuilder(() => new SqlConnection(connection));
            });
    }
}