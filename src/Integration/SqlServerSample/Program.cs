using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

class Program
{
    static Task Main()
    {
        LogManager.Use<DefaultFactory>().Level( LogLevel.Info);
        var connection = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencesample;Integrated Security=True;MultipleActiveResultSets=True";
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