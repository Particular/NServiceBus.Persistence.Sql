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
        var sagaWithBadOverride = new SagaWithBadOverride();
        var exception = Assert.Throws<Exception>(() =>
        {
            sagaWithBadOverride.VerifyNoConfigureHowToFindSaga();
        });
        Approvals.Verify(exception.Message);
    }

    public class SagaWithBadOverride : SqlSaga<SagaWithBadOverride.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }

        protected override string CorrelationPropertyName { get; }

        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage mapper)
        {
        }
    }

    [Test]
    public void WithGoodOverride()
    {
        new SagaWithGoodOverride().VerifyNoConfigureHowToFindSaga();
    }

    public class SagaWithGoodOverride : SqlSaga<SagaWithGoodOverride.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override string CorrelationPropertyName { get; }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }
}