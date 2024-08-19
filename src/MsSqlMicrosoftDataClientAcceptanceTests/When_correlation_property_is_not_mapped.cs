﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class When_correlation_property_is_not_mapped : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_throw_validation_exception()
    {
        if (!MsSqlMicrosoftDataClientConnectionBuilder.IsSql2016OrHigher())
        {
            return;
        }
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b
                    .When(session =>
                    {
                        var startSagaMessage = new StartSagaMessage
                        {
                            Property = "Test"
                        };
                        return session.SendLocal(startSagaMessage);
                    }))
                .Done(c => c.StartSagaFinderUsed)
                .Run());

        Assert.That(exception.Message, Is.EqualTo("The saga 'When_correlation_property_is_not_mapped+SagaEndpoint+TestSaga' defines a correlation property 'Property' which is not mapped to any message. Either map it or remove it from the saga definition."));
    }

    public class Context : ScenarioContext
    {
        public bool StartSagaFinderUsed { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class FindByStartSagaMessage : ISagaFinder<TestSaga.SagaData, StartSagaMessage>
        {
            Context testContext;

            public FindByStartSagaMessage(Context context)
            {
                testContext = context;
            }

            public Task<TestSaga.SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession session, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
            {
                testContext.StartSagaFinderUsed = true;

                return session.GetSagaData<TestSaga.SagaData>(
                    context: context,
                    whereClause: "json_value(Data,'$.Property') = @propertyValue",
                    appendParameters: (builder, append) =>
                    {
                        var parameter = builder();
                        parameter.ParameterName = "propertyValue";
                        parameter.Value = "Test";
                        append(parameter);
                    }, cancellationToken);
            }
        }

        public class TestSaga : SqlSaga<TestSaga.SagaData>,
            IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.Property = message.Property;
                return Task.CompletedTask;
            }

            protected override string CorrelationPropertyName => nameof(SagaData.Property);

            protected override void ConfigureMapping(IMessagePropertyMapper mapper)
            {
            }

            public class SagaData : ContainSagaData
            {
                public string Property { get; set; }
            }
        }
    }

    public class StartSagaMessage : IMessage
    {
        public string Property { get; set; }
    }
}