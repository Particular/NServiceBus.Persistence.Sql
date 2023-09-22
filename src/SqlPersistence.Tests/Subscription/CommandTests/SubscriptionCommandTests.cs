using NServiceBus;
using NUnit.Framework;
using Particular.Approvals;

public abstract class SubscriptionCommandTests
{
    SqlDialect sqlDialect;

    public SubscriptionCommandTests(SqlDialect sqlDialect)
    {
        this.sqlDialect = sqlDialect;
    }

    [Test]
    public void GetSubscribers()
    {
        var timeoutCommands = SubscriptionCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        // TODO: Not much point validating a type name
        Approver.Verify(timeoutCommands.GetSubscribers.ToString(), scenario: GetType().Name);
    }

    [Test]
    public void Subscribe()
    {
        var timeoutCommands = SubscriptionCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Subscribe, scenario: GetType().Name);
    }

    [Test]
    public void Unsubscribe()
    {
        var timeoutCommands = SubscriptionCommandBuilder.Build(sqlDialect, "TheTablePrefix");

        Approver.Verify(timeoutCommands.Unsubscribe, scenario: GetType().Name);
    }

    [TestFixture]
    public class MsSql : SubscriptionCommandTests
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
    public class Oracle : SubscriptionCommandTests
    {
        public Oracle() :
            base(new SqlDialect.Oracle())
        {
        }
    }

    [TestFixture]
    public class MySql : SubscriptionCommandTests
    {
        public MySql() :
            base(new SqlDialect.MySql())
        {
        }
    }

    [TestFixture]
    public class PostgreSql : SubscriptionCommandTests
    {
        public PostgreSql() :
            base(new SqlDialect.PostgreSql())
        {
        }
    }
}