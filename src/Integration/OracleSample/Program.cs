using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using Oracle.ManagedDataAccess.Client;

class Program
{
    static void Main()
    {
        Start().GetAwaiter().GetResult();
    }

    static Task Start()
    {
        var connection = "Data Source=localhost;User Id=particular; Password=Welcome1; Enlist=false";
        return EndpointStarter.Start("SqlPersistence.Sample.Oracle",
            persistence =>
            {
                persistence.SqlVariant(SqlVariant.Oracle);
                persistence.TablePrefix("Oracle");
                persistence.ConnectionBuilder(() => new OracleConnection(connection));
            });
    }
}