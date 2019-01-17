#pragma warning disable 618
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using Particular.Approvals;

public abstract class OutboxCommandTests
{
    SqlDialect sqlDialect;

    public OutboxCommandTests(SqlDialect sqlDialect)
    {
        this.sqlDialect = sqlDialect;
    }

    [Test]
    public void Get()
    {
        var timeoutCommands = OutboxCommandBuilder.Build("TheTablePrefix", sqlDialect);

        Approver.Verify(timeoutCommands.Get, scenario: GetType().Name);
    }

    [Test]
    public void Cleanup()
    {
        var timeoutCommands = OutboxCommandBuilder.Build("TheTablePrefix", sqlDialect);

        Approver.Verify(timeoutCommands.Cleanup, scenario: GetType().Name);
    }

    [Test]
    public void SetAsDispatched()
    {
        var timeoutCommands = OutboxCommandBuilder.Build("TheTablePrefix", sqlDialect);

        Approver.Verify(timeoutCommands.SetAsDispatched, scenario: GetType().Name);
    }

    [Test]
    public void Store()
    {
        var timeoutCommands = OutboxCommandBuilder.Build("TheTablePrefix", sqlDialect);

        Approver.Verify(timeoutCommands.Store, scenario: GetType().Name);
    }

    [TestFixture]
    public class MsSql : OutboxCommandTests
    {
        public MsSql() :
            base(new SqlDialect.MsSqlServer
            {
                Schema = "TheSchema"
            })
        {
        }
    }

    [TestFixture]
    public class Oracle : OutboxCommandTests
    {
        public Oracle() :
            base(new SqlDialect.Oracle())
        {
        }
    }

    [TestFixture]
    public class MySql : OutboxCommandTests
    {
        public MySql() :
            base(new SqlDialect.MySql())
        {
        }
    }

    [TestFixture]
    public class PostgreSql : OutboxCommandTests
    {
        public PostgreSql() :
            base(new SqlDialect.PostgreSql())
        {
        }
    }
}