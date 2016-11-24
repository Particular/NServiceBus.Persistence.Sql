using System.Data.SqlClient;

namespace NServiceBus.Persistence.Sql
{
    public interface ISqlStorageSession
    {
        SqlTransaction Transaction { get; }
        SqlConnection Connection { get; }
    }
}