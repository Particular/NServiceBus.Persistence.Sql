using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Persistence.Sql;

class NoSqlStorageSession : ISqlStorageSession
{
    public static readonly NoSqlStorageSession Instance = new NoSqlStorageSession();
    public DbTransaction Transaction => throw new InvalidOperationException("ISqlStorageSession not available, no message context.");
    public DbConnection Connection => throw new InvalidOperationException("ISqlStorageSession not available, no message context.");
    public void OnSaveChanges(Func<ISqlStorageSession, Task> callback) => throw new InvalidOperationException("ISqlStorageSession not available, no message context.");
}
