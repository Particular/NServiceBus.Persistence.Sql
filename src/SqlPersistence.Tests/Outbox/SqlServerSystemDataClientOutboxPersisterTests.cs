namespace SqlServerSystemData
{
    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NUnit.Framework;

    [TestFixture(false, false)]
    [TestFixture(true, false)]
    [TestFixture(false, true)]
    [TestFixture(true, true)]
    [MsSqlOnly]
    public class SqlServerSystemDataClientOutboxPersisterTests : OutboxPersisterTests
    {
        public SqlServerSystemDataClientOutboxPersisterTests(bool pessimistic, bool transactionScope)
            : base(BuildSqlDialect.MsSqlServer, "schema_name", pessimistic, transactionScope)
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x => MsSqlSystemDataClientConnectionBuilder.Build();
        }
    }
}