namespace SqlServerMicrosoftData
{
    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NUnit.Framework;

    [TestFixture, SqlServerTest]
    public class SqlServerMicrosoftDataClientSagaPersisterTests : SagaPersisterTests
    {
        public SqlServerMicrosoftDataClientSagaPersisterTests() : base(BuildSqlDialect.MsSqlServer, "schema_name")
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x =>
            {
                var connection = MsSqlMicrosoftDataClientConnectionBuilder.Build();
                return connection;
            };
        }

        protected override string GetPropertyWhereClauseExists(string schema, string table, string propertyName)
        {
            return $@"
select 1 from sys.columns
where Name = N'{propertyName}'
and Object_ID = Object_ID(N'{schema}.{table}')
";
        }

        protected override bool IsConcurrencyException(Exception innerException)
        {
            return innerException.Message.Contains("Cannot insert duplicate key row in object ");
        }
    }
}