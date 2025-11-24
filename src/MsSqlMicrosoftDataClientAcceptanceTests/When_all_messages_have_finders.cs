using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class When_all_messages_have_finders : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_existing_saga()
    {
        if (!MsSqlMicrosoftDataClientConnectionBuilder.IsSql2016OrHigher())
        {
            return;
        }

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b
                .When(session =>
                {
                    var startSagaMessage = new StartSagaMessage { Property = "Test" };
                    return session.SendLocal(startSagaMessage);
                }))
            .Done(c => c.HandledOtherMessage)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.StartSagaFinderUsed, Is.True);
            Assert.That(context.SomeOtherFinderUsed, Is.True);
        });
    }

    public class Context : ScenarioContext
    {
        public bool StartSagaFinderUsed { get; set; }
        public bool HandledOtherMessage { get; set; }
        public bool SomeOtherFinderUsed { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() => EndpointSetup<DefaultServer>();

        public class FindByStartSagaMessage(Context testContext) : ISagaFinder<TestSaga.SagaData, StartSagaMessage>
        {
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

        public class FindBySomeOtherMessage(Context testContext) : ISagaFinder<TestSaga.SagaData, SomeOtherMessage>
        {
            public Task<TestSaga.SagaData> FindBy(SomeOtherMessage message, ISynchronizedStorageSession session, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
            {
                testContext.SomeOtherFinderUsed = true;

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

        public class TestSaga(Context testContext) : Saga<TestSaga.SagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<SomeOtherMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.Property = message.Property;
                return context.SendLocal(new SomeOtherMessage { Property = Data.Property });
            }

            public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
            {
                testContext.HandledOtherMessage = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureFinderMapping<StartSagaMessage, FindByStartSagaMessage>();
                mapper.ConfigureFinderMapping<SomeOtherMessage, FindBySomeOtherMessage>();
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

    public class SomeOtherMessage : IMessage
    {
        public string Property { get; set; }
    }
}