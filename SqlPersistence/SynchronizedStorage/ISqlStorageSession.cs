using System.Data.Common;

namespace NServiceBus.Persistence.Sql
{
    public interface ISqlStorageSession
    {
        DbTransaction Transaction { get; }
        DbConnection Connection { get; }
    }
}