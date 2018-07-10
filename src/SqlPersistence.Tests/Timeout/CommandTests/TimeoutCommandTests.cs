#pragma warning disable 618
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using Particular.Approvals;

public abstract class TimeoutCommandTests
{
    SqlDialect sqlDialect;

    public TimeoutCommandTests(SqlDialect sqlDialect)
    {
        this.sqlDialect = sqlDialect;
    }

    [Test]
    public void Add()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Add, category: GetType().Name);
    }

    [Test]
    public void Next()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Next, category: GetType().Name);
    }

    [Test]
    public void Peek()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Peek, category: GetType().Name);
    }

    [Test]
    public void Range()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Range, category: GetType().Name);
    }

    [Test]
    public void RemoveById()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.RemoveById, category: GetType().Name);
    }

    [Test]
    public void RemoveBySagaId()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.RemoveBySagaId, category: GetType().Name);
    }

    [TestFixture]
    public class MsSql : TimeoutCommandTests
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
    public class Oracle : TimeoutCommandTests
    {
        public Oracle() :
            base(new SqlDialect.Oracle())
        {
        }
    }

    [TestFixture]
    public class MySql : TimeoutCommandTests
    {
        public MySql() :
            base(new SqlDialect.MySql())
        {
        }
    }

    [TestFixture]
    public class PostgreSql : TimeoutCommandTests
    {
        public PostgreSql() :
            base(new SqlDialect.PostgreSql())
        {
        }
    }
}