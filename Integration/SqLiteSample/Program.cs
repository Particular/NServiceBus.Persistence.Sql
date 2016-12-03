using System.Data.SQLite;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static void Main()
    {
        Start().GetAwaiter().GetResult();
    }

    static Task Start()
    {
        SQLiteConnection.CreateFile("MyDatabase.sqlite");
        return EndpointStarter.Start("SqlPersistence.Sample.SqlLite", persistence =>
        {
            persistence.ConnectionBuilder(async () =>
            {
                var connection = new SQLiteConnection("Data Source=MyDatabase.sqlite");
                await connection.OpenAsync();
                return connection;
            });
        });
    }
}