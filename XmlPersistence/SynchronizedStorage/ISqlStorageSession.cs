using System.Data.SqlClient;

namespace NServiceBus.Persistence.Sql.Xml
{
    public interface ISqlStorageSession
    {
        SqlTransaction Transaction { get; }
        SqlConnection Connection { get; }
    }
}