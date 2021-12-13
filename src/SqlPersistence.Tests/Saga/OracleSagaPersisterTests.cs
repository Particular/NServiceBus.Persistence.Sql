namespace Oracle
{
    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;
    using NUnit.Framework;
    using Oracle.ManagedDataAccess.Client;

    [TestFixture, OnlyOnWindows, OracleOnly]
    public class OracleSagaPersisterTests : SagaPersisterTests
    {
        public OracleSagaPersisterTests() : base(BuildSqlDialect.Oracle, "Particular2")
        {
        }

        protected override bool SupportsSchemas() => true;

        protected override Func<string, DbConnection> GetConnection()
        {
            return schema =>
            {
                var key = schema == null
                    ? "OracleConnectionString"
                    : $"OracleConnectionString_{schema}";

                var connectionString = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new Exception($"{key} environment variable is empty");
                }

                var connection = new OracleConnection(connectionString);
                connection.Open();
                return connection;
            };
        }

        protected override string TestTableName(string testName, string tableSuffix)
        {
            return $"{tableSuffix.ToUpper()}";
        }

        protected override string CorrelationPropertyName(string propertyName)
        {
            return $"CORR_{propertyName.ToUpper()}";
        }

        protected override string GetPropertyWhereClauseExists(string schema, string table, string propertyName)
        {
            return $@"
select count(*)
from all_tab_columns
where table_name = '{table}' and
      column_name = '{propertyName}'
";
        }

        protected override bool IsConcurrencyException(Exception innerException)
        {
            // ORA-00001: unique constraint (TESTUSER.SAGAWITHCORRELATION_CP) violated
            return innerException.Message.Contains("ORA-00001");
        }

        protected override bool SupportsUnicodeIdentifiers { get; } = false;

        protected override bool LoadTypeForSagaMetadata(Type type)
        {
            if (type == typeof(SagaWithWeirdCharactersಠ_ಠ))
            {
                return false;
            }

            return true;
        }
    }
}