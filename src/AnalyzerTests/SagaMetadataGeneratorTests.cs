namespace AnalyzerTests;

using Microsoft.CodeAnalysis;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.Analyzer;
using NUnit.Framework;

public class SagaMetadataGeneratorTests
{
    [Test]
    public void BasicSaga()
    {
        var code = $$"""
                   using System.Threading.Tasks;
                   using NServiceBus;
                   
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
                   }
                   public record class StartOrder(string OrderId);
                   """;

        SourceGeneratorTest.ForIncrementalGenerator<SagaMetadataGenerator>()
            .WithSource(code)
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
}