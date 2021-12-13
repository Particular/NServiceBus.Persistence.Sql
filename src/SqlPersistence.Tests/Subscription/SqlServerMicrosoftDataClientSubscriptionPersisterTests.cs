namespace SqlServerMicrosoftData
{
    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NUnit.Framework;

    [TestFixture, MsSqlOnly]
    public class SqlServerMicrosoftDataClientSubscriptionPersisterTests : SubscriptionPersisterTests
    {
        public SqlServerMicrosoftDataClientSubscriptionPersisterTests() : base(BuildSqlDialect.MsSqlServer,
            "schema_name")
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x => MsSqlMicrosoftDataClientConnectionBuilder.Build();
        }
    }
}