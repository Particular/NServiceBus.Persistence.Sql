using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NServiceBus;

class Program
{
    static Task Main()
    {
        var connection = "server=localhost;user=root;database=sqlpersistencesample;port=3306;password=Password1;Allow User Variables=True";
        return EndpointStarter.Start("SqlPersistence.Sample.MySql",
            persistence =>
            {
                persistence.SqlDialect<SqlDialect.MySql>();
                persistence.TablePrefix("MySql");
                persistence.ConnectionBuilder(() => new MySqlConnection(connection));
            });
    }
}