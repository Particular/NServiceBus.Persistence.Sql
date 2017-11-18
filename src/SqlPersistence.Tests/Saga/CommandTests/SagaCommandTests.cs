#if NET452
#pragma warning disable 618
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus;
using NUnit.Framework;

public abstract class SagaCommandTests
{
    SqlDialect sqlDialect;

    public SagaCommandTests(SqlDialect sqlDialect)
    {
        this.sqlDialect = sqlDialect;
    }

    [Test]
    public void Complete()
    {
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sqlDialect.BuildCompleteCommand("TheTableName"));
        }
    }

    [Test]
    public void GetByProperty()
    {
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sqlDialect.BuildGetByPropertyCommand("ThePropertyName", "TheTableName"));
        }
    }

    [Test]
    public void GetBySagaId()
    {
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sqlDialect.BuildGetBySagaIdCommand("TheTableName"));
        }
    }

    [Test]
    public void Save()
    {
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sqlDialect.BuildSaveCommand("CorrelationName", "TransitionalName", "TheTableName"));
        }
    }

    [Test]
    public void SelectFrom()
    {
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sqlDialect.BuildSelectFromCommand("TheTableName"));
        }
    }

    [Test]
    public void Update()
    {
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sqlDialect.BuildUpdateCommand("TransitionalName", "TheTableName"));
        }
    }

    [TestFixture]
    public class MsSql : SagaCommandTests
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
    public class Oracle : SagaCommandTests
    {
        public Oracle() :
            base(new SqlDialect.Oracle())
        {
        }
    }

    [TestFixture]
    public class MySql : SagaCommandTests
    {
        public MySql() :
            base(new SqlDialect.MySql())
        {
        }
    }

    [TestFixture]
    public class PostgreSql : SagaCommandTests
    {
        public PostgreSql() :
            base(new SqlDialect.PostgreSql())
        {
        }
    }
}
#endif