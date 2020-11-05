namespace SqlServerMicrosoftData
{
    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NUnit.Framework;

    [TestFixture(false, false)]
    [TestFixture(true, false)]
    [TestFixture(false, true)]
    [TestFixture(true, true)]
    public class SqlServerMicrosoftDataClientOutboxPersisterTests : OutboxPersisterTests
    {
        public SqlServerMicrosoftDataClientOutboxPersisterTests(bool pessimistic, bool transactionScope)
            : base(BuildSqlDialect.MsSqlServer, "schema_name", pessimistic, transactionScope)
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x => MsSqlMicrosoftDataClientConnectionBuilder.Build();
        }
    }
}