using System.IO;
using System.Text;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class SagaTimeoutScriptBuilderTest
{
    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.Oracle)]
    [TestCase(BuildSqlDialect.PostgreSql)]
    public void BuildCreateScript(BuildSqlDialect sqlDialect)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaTimeoutScriptBuilder.BuildCreateScript(writer, sqlDialect);
        }
        var script = builder.ToString();
        if (sqlDialect == BuildSqlDialect.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }

        Assert.That(script, Is.EqualTo(script.ReplaceLineEndings()), "Line endings are not consistent");

        Approver.Verify(script, scenario: "ForScenario." + sqlDialect);
    }

    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.Oracle)]
    [TestCase(BuildSqlDialect.PostgreSql)]
    public void BuildDropScript(BuildSqlDialect sqlDialect)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaTimeoutScriptBuilder.BuildDropScript(writer, sqlDialect);
        }
        var script = builder.ToString();
        if (sqlDialect == BuildSqlDialect.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }

        Assert.That(script, Is.EqualTo(script.ReplaceLineEndings()), "Line endings are not consistent");

        Approver.Verify(script, scenario: "ForScenario." + sqlDialect);
    }

    [TestCase(BuildSqlDialect.MsSqlServer, "TimeoutId nvarchar(200) collate Latin1_General_100_BIN2", "Data nvarchar(max)")]
    [TestCase(BuildSqlDialect.MySql, "TimeoutId varchar(200) character set utf8mb4 collate utf8mb4_bin", "Data json")]
    public void SchemaPreservesUnicodeAndCaseSensitiveTimeoutIds(BuildSqlDialect sqlDialect, string timeoutIdColumn, string dataColumn)
    {
        var script = SagaTimeoutScriptBuilder.BuildCreateScript(sqlDialect);
        Assert.That(script, Does.Contain(timeoutIdColumn).And.Contain(dataColumn));
    }

    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer, "SagaTimeoutData")]
    [TestCase(BuildSqlDialect.MySql, "SagaTimeoutData")]
    [TestCase(BuildSqlDialect.Oracle, "'ST'")]
    [TestCase(BuildSqlDialect.PostgreSql, "SagaTimeoutData")]
    public void ScriptsTargetOnlySagaTimeoutTable(BuildSqlDialect sqlDialect, string sagaTimeoutTable)
    {
        var legacyTable = sqlDialect == BuildSqlDialect.Oracle ? "'TO'" : "(?<!Saga)TimeoutData";

        using (Assert.EnterMultipleScope())
        {
            foreach (var script in new[] { SagaTimeoutScriptBuilder.BuildCreateScript(sqlDialect), SagaTimeoutScriptBuilder.BuildDropScript(sqlDialect) })
            {
                Assert.That(script, Does.Contain(sagaTimeoutTable));
                Assert.That(script, Does.Not.Match(legacyTable));
            }

            // The legacy builder keeps its own table, so saga timeout cleanup can never hit it.
            Assert.That(TimeoutScriptBuilder.BuildDropScript(sqlDialect), Does.Not.Contain(sagaTimeoutTable));
            Assert.That(TimeoutScriptBuilder.BuildCreateScript(sqlDialect), Does.Not.Contain(sagaTimeoutTable));
        }
    }
}
