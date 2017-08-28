using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static Task Main()
    {
        var connection = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencesample;Integrated Security=True";
        SqlHelper.EnsureDatabaseExists(connection);
        return EndpointStarter.Start("SqlPersistence.Sample.MsSqlServer",
            persistence =>
            {
                persistence.SqlDialect<SqlDialect.MsSqlServer>();
                persistence.TablePrefix("AcceptanceTests");
                persistence.ConnectionBuilder(() => new SqlConnection(connection));
            });
    }
}