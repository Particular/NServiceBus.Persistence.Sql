#pragma warning disable 618
using NServiceBus;
using NUnit.Framework;
using Particular.Approvals;

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
        Approver.Verify(sqlDialect.BuildCompleteCommand("TheTableName"), category: GetType().Name);
    }

    [Test]
    public void GetByProperty()
    {
        Approver.Verify(sqlDialect.BuildGetByPropertyCommand("ThePropertyName", "TheTableName"), category: GetType().Name);
    }

    [Test]
    public void GetBySagaId()
    {
        Approver.Verify(sqlDialect.BuildGetBySagaIdCommand("TheTableName"), category: GetType().Name);
    }

    [Test]
    public void Save()
    {
        Approver.Verify(sqlDialect.BuildSaveCommand("CorrelationName", "TransitionalName", "TheTableName"), category: GetType().Name);
    }

    [Test]
    public void SelectFrom()
    {
        Approver.Verify(sqlDialect.BuildSelectFromCommand("TheTableName")("1 = 1"), category: GetType().Name);
    }

    [Test]
    public void Update()
    {
        Approver.Verify(sqlDialect.BuildUpdateCommand("TransitionalName", "TheTableName"), category: GetType().Name);
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