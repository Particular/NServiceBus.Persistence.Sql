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
        var connection = "server=localhost;user=root;database=sqlpersistencesample;port=3306;password=Password1;Allow User Variables=True";
        return EndpointStarter.Start("SqlPersistence.Sample.MySql",
            persistence =>
            {
                persistence.SqlVariant(SqlVariant.MySql);
                persistence.TablePrefix("MySql");
                persistence.ConnectionBuilder(() => new MySqlConnection(connection));
            });
    }
}