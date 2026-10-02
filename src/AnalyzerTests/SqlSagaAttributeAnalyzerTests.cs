namespace AnalyzerTests;

using Microsoft.CodeAnalysis;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.Analyzer;
using NUnit.Framework;
using Particular.AnalyzerTesting;

public class SqlSagaAttributeAnalyzerTests : AnalyzerTestFixture<SqlSagaAttributeAnalyzer>
{
    [SetUp]
    public void LoadDependencies()
    {
        MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location);
        MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location);
        MetadataReference.CreateFromFile(typeof(SqlSagaAttribute).Assembly.Location);
    }

    [Test]
    public Task ReportsMissingCorrelationProperty()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(correlationProperty: [|"MissingCorrelation"|])]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code, DiagnosticIds.CorrelationPropertyNotFound);
    }

    [Test]
    public Task ReportsWhenUsingWrongNameof()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(correlationProperty: [|nameof(WrongClass.MissingCorrelation)|])]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            
                            public record class WrongClass(string MissingCorrelation);
                            """;

        return Assert(code, DiagnosticIds.CorrelationPropertyNotFound);
    }

    [Test]
    public Task ReportsMissingTransitionalCorrelationProperty()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(transitionalCorrelationProperty: [|"MissingTransitionalCorrelation"|])]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code, DiagnosticIds.CorrelationPropertyNotFound);
    }

    [Test]
    public Task ReportsWhenBothCorrelationPropertiesMissing()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(correlationProperty: [|"MissingCorrelationProperty"|], transitionalCorrelationProperty: [|"MissingTransitionalCorrelation"|])]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code, DiagnosticIds.CorrelationPropertyNotFound);
    }

    [Test]
    public Task ReportsWhenBothCorrelationPropertiesMissingPlusTableSuffix()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(tableSuffix: "TableSuffix", correlationProperty: [|"MissingCorrelationProperty"|], transitionalCorrelationProperty: [|"MissingTransitionalCorrelation"|])]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code, DiagnosticIds.CorrelationPropertyNotFound);
    }

    [Test]
    public Task DoesNotReportWhenPropertyIsDefined()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(correlationProperty: nameof(OrderSagaData.OrderId))]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code);
    }

    [Test]
    public Task DoesNotReportWhenPropertyIsDefinedOnBaseClass()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(correlationProperty: nameof(BaseOrderSagaData.OrderId))]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : BaseOrderSagaData
                            {
                            }

                            public class BaseOrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code);
    }

    [Test]
    public Task DoesNotReportForTableSuffix()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga(tableSuffix: "CustomTableSuffix")]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code);
    }

    [Test]
    public Task ReportsCorrectlyWhenOnlyPositionalValuesUsed()
    {
        const string code = """
                            using NServiceBus;
                            using NServiceBus.Persistence.Sql;

                            [SqlSaga([|"MissingCorrelationProperty"|], [|"MissingTransitionalCorrelation"|], "CustomTableSuffix")]
                            public class OrderSaga : Saga<OrderSagaData>
                            {
                                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                                {
                                    mapper.MapSaga(saga => saga.OrderId)
                                        .ToMessage<StartOrder>(message => message.OrderId);
                                }
                            }

                            public class OrderSagaData : ContainSagaData
                            {
                                public string OrderId { get; set; }
                            }

                            public record class StartOrder(string OrderId);
                            """;

        return Assert(code, DiagnosticIds.CorrelationPropertyNotFound);
    }
}
