using System.Data.SqlClient;

namespace NServiceBus.Persistence.SqlServerXml
{
    public interface ISqlStorageSession
    {
        SqlTransaction Transaction { get; }
        SqlConnection Connection { get; }
    }
}