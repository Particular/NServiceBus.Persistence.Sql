using NServiceBus;
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
        var timeoutCommands = OutboxCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Get, scenario: GetType().Name);
    }

    [Test]
    public void Cleanup()
    {
        var timeoutCommands = OutboxCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Cleanup, scenario: GetType().Name);
    }

    [Test]
    public void SetAsDispatched()
    {
        var timeoutCommands = OutboxCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.SetAsDispatched, scenario: GetType().Name);
    }

    [Test]
    public void Store()
    {
        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(outboxCommands.OptimisticStore, scenario: GetType().Name);
    }

    [Test]
    public void Begin()
    {
        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(outboxCommands.PessimisticBegin, scenario: GetType().Name);
    }

    [Test]
    public void Complete()
    {
        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(outboxCommands.PessimisticComplete, scenario: GetType().Name);
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