#pragma warning disable 618
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

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
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.Add);
        }
    }

    [Test]
    public void Next()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.Next);
        }
    }

    [Test]
    public void Peek()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.Peek);
        }
    }

    [Test]
    public void Range()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.Range);
        }
    }

    [Test]
    public void RemoveById()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.RemoveById);
        }
    }

    [Test]
    public void RemoveBySagaId()
    {
        var timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, "TheTablePrefix");
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.RemoveBySagaId);
        }
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