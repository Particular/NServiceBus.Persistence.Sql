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
            var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencesample;Integrated Security=True";
            persistence.SqlVarient(SqlVarient.MsSqlServer); 
            persistence.TablePrefix("AcceptanceTests");  
            persistence.ConnectionBuilder(() => new SqlConnection(connectionString));
        });
    }
}