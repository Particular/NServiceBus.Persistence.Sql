using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OracleSagaPersisterTests : SagaPersisterTests
{
    public OracleSagaPersisterTests() : base(BuildSqlDialect.Oracle, null)
    {
    }

    protected override bool SupportsSchemas() => false;

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = OracleConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }

    protected override string GetPropertyWhereClauseExists(string schema, string table, string propertyName)
    {
        return $@"
select count(*)
from all_tab_columns
where table_name = '{table}' and
      column_name = '{propertyName}';
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