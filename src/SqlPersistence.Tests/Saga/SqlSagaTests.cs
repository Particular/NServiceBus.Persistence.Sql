using System;
using ApprovalTests;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class SqlSagaTests
{
    [Test]
    public void WithBadOverride()
    {
        var exception = Assert.Throws<Exception>(() => new SagaWithBadOverride());
        Approvals.Verify(exception.Message);
    }

    public class SagaWithBadOverride : SqlSaga<SagaWithBadOverride.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }

    }

    [Test]
    public void WithGoodOverride()
    {
        new SagaWithGoodOverride();
    }

    public class SagaWithGoodOverride : SqlSaga<SagaWithGoodOverride.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }
}