namespace PostgreSql
{

    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NUnit.Framework;

    [TestFixture]
    public class PostgreSqlSubscriptionPersisterTests : SubscriptionPersisterTests
    {
        public PostgreSqlSubscriptionPersisterTests() : base(BuildSqlDialect.PostgreSql, "SchemaName")
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x => PostgreSqlConnectionBuilder.Build();
        }
    }
}