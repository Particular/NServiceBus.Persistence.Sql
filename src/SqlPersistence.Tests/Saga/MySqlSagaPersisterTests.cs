using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MySqlSagaPersisterTests: SagaPersisterTests
{
    public MySqlSagaPersisterTests() : base(BuildSqlVariant.MySql)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = MySqlConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }
    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Duplicate entry ");
    }

    // So this test can be excluded if a target server install does not support unicode
    [Category("MySqlUnicode")]
    public override void SaveWithWeirdCharacters()
    {
        base.SaveWithWeirdCharacters();
    }
}