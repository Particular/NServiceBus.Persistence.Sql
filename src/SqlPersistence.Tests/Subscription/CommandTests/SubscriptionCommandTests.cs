#pragma warning disable 618
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

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
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.GetSubscribers);
        }
    }

    [Test]
    public void Subscribe()
    {
        var timeoutCommands = SubscriptionCommandBuilder.Build(sqlDialect, "TheTablePrefix");
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.Subscribe);
        }
    }

    [Test]
    public void Unsubscribe()
    {
        var timeoutCommands = SubscriptionCommandBuilder.Build(sqlDialect, "TheTablePrefix");
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(timeoutCommands.Unsubscribe);
        }
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
}