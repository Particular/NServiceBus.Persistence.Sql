using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class SagaDefinitionValidatorTest
{
    [Test]
    public void Simple()
    {
        SagaDefinitionValidator.ValidateSagaDefinition("Correlation", "saga1", "Transitional", "tableSuffix");
    }

    [Test]
    public void WithMatchingIds()
    {
        var errorsException = Assert.Throws<ErrorsException>(() => SagaDefinitionValidator.ValidateSagaDefinition("Transitional", "saga1", "Transitional", "tableSuffix"));
        Assert.That(errorsException.Message, Is.Not.Null);
        Approver.Verify(errorsException.Message);
    }

    [Test]
    public void WithInvalidSuffixLeft()
    {
        var errorsException = Assert.Throws<ErrorsException>(() => SagaDefinitionValidator.ValidateSagaDefinition("Correlation", "saga1", "Transitional", "table[Suffix"));
        Assert.That(errorsException.Message, Is.Not.Null);
        Approver.Verify(errorsException.Message);
    }

    [Test]
    public void WithInvalidSuffixRight()
    {
        var errorsException = Assert.Throws<ErrorsException>(() => SagaDefinitionValidator.ValidateSagaDefinition("Correlation", "saga1", "Transitional", "table]Suffix"));
        Assert.That(errorsException.Message, Is.Not.Null);
        Approver.Verify(errorsException.Message);
    }

    [Test]
    public void WithInvalidSuffixTick()
    {
        var errorsException = Assert.Throws<ErrorsException>(() => SagaDefinitionValidator.ValidateSagaDefinition("Correlation", "saga1", "Transitional", "table`Suffix"));
        Assert.That(errorsException.Message, Is.Not.Null);
        Approver.Verify(errorsException.Message);
    }

    [Test]
    public void WithNoCorrelation()
    {
        SagaDefinitionValidator.ValidateSagaDefinition(null, "saga1", null, "tableSuffix");
    }

    [Test]
    public void WithNoTransitionalCorrelation()
    {
        SagaDefinitionValidator.ValidateSagaDefinition("Correlation", "saga1", null, "tableSuffix");
    }

    [SqlSaga(tableSuffix: "tableSuffix", correlationProperty: nameof(SagaData.Correlation))]
    public class WithNoTransitionalCorrelationSaga : Saga<WithNoTransitionalCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => throw new System.NotImplementedException();
    }

    [Test]
    public void WithTransitionalAndNoCorrelation()
    {
        var errorsException = Assert.Throws<ErrorsException>(() => SagaDefinitionValidator.ValidateSagaDefinition(null, "saga1", "Transitional", "tableSuffix"));
        Assert.That(errorsException.Message, Is.Not.Null);
        Approver.Verify(errorsException.Message);
    }
}