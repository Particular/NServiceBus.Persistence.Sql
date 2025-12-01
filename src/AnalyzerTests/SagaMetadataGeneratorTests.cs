namespace AnalyzerTests;

using Microsoft.CodeAnalysis;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.Analyzer;
using NUnit.Framework;

public class SagaMetadataGeneratorTests
{
    [Test]
    [TestCase("string")]
    [TestCase("long")]
    [TestCase("ulong")]
    [TestCase("int")]
    [TestCase("uint")]
    [TestCase("short")]
    [TestCase("ushort")]
    [TestCase("Guid")]

    public void BasicSaga(string correlationType)
    {
        var usingSystem = correlationType == "Guid" ? "using System;" : "";
        var code = $$"""
                   {{usingSystem}}
                   using System.Threading.Tasks;
                   using NServiceBus;
                   
                   namespace My.NameSpace;
                   
                   public class OrderSaga : Saga<OrderSagaData>, IAmStartedByMessages<StartOrder>
                   {
                       protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                       {
                           mapper.MapSaga(saga => saga.OrderId)
                               .ToMessage<StartOrder>(message => message.OrderId);
                       }
                       public Task Handle(StartOrder message, IMessageHandlerContext context) => Task.CompletedTask;
                   }
                   public class OrderSagaData : ContainSagaData
                   {
                       public {{correlationType}} OrderId { get; set; }
                   }
                   public record class StartOrder({{correlationType}} OrderId);
                   """;

        SourceGeneratorTest.ForIncrementalGenerator<SagaMetadataGenerator>()
            .WithSource(code)
            .WithScenarioName(correlationType)
            .WithGeneratorStages("Candidates", "Collected")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void WithTransitionalCorrelationId()
    {
        var code = $$"""
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using NServiceBus.Persistence.Sql;

                     [SqlSaga(transitionalCorrelationProperty: nameof(OrderSagaData.TransitionalId))]
                     public class OrderSaga : Saga<OrderSagaData>, IAmStartedByMessages<StartOrder>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                 .ToMessage<StartOrder>(message => message.OrderId);
                         }
                         public Task Handle(StartOrder message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                         public string TransitionalId { get; set; }
                     }
                     public record class StartOrder(string OrderId);
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<SagaMetadataGenerator>()
            .WithSource(code)
            .AddReference(MetadataReference.CreateFromFile(typeof(SqlSagaAttribute).Assembly.Location))
            .WithGeneratorStages("Candidates", "Collected")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void OnlyFindersNoCorrelation()
    {
        var code = $$"""
                     using System.Threading;
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using NServiceBus.Extensibility;
                     using NServiceBus.Persistence;
                     using NServiceBus.Sagas;

                     namespace My.NameSpace;

                     public class OrderSaga : Saga<OrderSagaData>, IAmStartedByMessages<StartOrder>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.ConfigureFinderMapping<StartOrder, FindByStartOrder>();
                         }
                         public Task Handle(StartOrder message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     public record class StartOrder(string OrderId);
                     public class FindByStartOrder : ISagaFinder<OrderSagaData, StartOrder>
                     {
                         public Task<OrderSagaData> FindBy(StartOrder message, ISynchronizedStorageSession session, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
                         {
                             // Completely invalid in real life but simple for the test
                             return Task.FromResult(new OrderSagaData());
                         }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<SagaMetadataGenerator>()
            .WithSource(code)
            .WithGeneratorStages("Candidates", "Collected")
            .ToConsole()
            //.Approve()
            .AssertRunsAreEqual();
    }
}