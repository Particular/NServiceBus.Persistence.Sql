#pragma warning disable 618
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus;
using NServiceBus.Persistence.Sql;
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
        var sagaCommandBuilder = new SagaCommandBuilder(sqlDialect);
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sagaCommandBuilder.BuildCompleteCommand("TheTableName"));
        }
    }

    [Test]
    public void GetByProperty()
    {
        var sagaCommandBuilder = new SagaCommandBuilder(sqlDialect);
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sagaCommandBuilder.BuildGetByPropertyCommand("ThePropertyName", "TheTableName"));
        }
    }

    [Test]
    public void GetBySagaId()
    {
        var sagaCommandBuilder = new SagaCommandBuilder(sqlDialect);
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sagaCommandBuilder.BuildGetBySagaIdCommand("TheTableName"));
        }
    }

    [Test]
    public void Save()
    {
        var sagaCommandBuilder = new SagaCommandBuilder(sqlDialect);
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sagaCommandBuilder.BuildSaveCommand("CorrelationName", "TransitionalName", "TheTableName"));
        }
    }

    [Test]
    public void SelectFrom()
    {
        var sagaCommandBuilder = new SagaCommandBuilder(sqlDialect);
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sagaCommandBuilder.BuildSelectFromCommand("TheTableName"));
        }
    }

    [Test]
    public void Update()
    {
        var sagaCommandBuilder = new SagaCommandBuilder(sqlDialect);
        using (NamerFactory.AsEnvironmentSpecificTest(() => GetType().Name))
        {
            Approvals.Verify(sagaCommandBuilder.BuildUpdateCommand("TransitionalName", "TheTableName"));
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
}