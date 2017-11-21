using System.Threading.Tasks;
using NServiceBus;
using Oracle.ManagedDataAccess.Client;

class Program
{
    static Task Main()
    {
        var connection = "Data Source=localhost;User Id=particular; Password=Welcome1; Enlist=false";
        return EndpointStarter.Start("SqlPersistence.Sample.Oracle",
            persistence =>
            {
                persistence.SqlDialect<SqlDialect.Oracle>();
                persistence.TablePrefix("Oracle");
                persistence.ConnectionBuilder(() => new OracleConnection(connection));
            });
    }
}