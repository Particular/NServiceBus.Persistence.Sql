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
public class When_all_messages_have_finders : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_existing_saga()
    {
        if (!MsSqlConnectionBuilder.IsSql2016OrHigher())
        {
            return;
        }
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b
                .When(session =>
                {
                    var startSagaMessage = new StartSagaMessage
                    {
                        Property = "Test"
                    };
                    return session.SendLocal(startSagaMessage);
                }))
            .Done(c => c.HandledOtherMessage)
            .Run()
            .ConfigureAwait(false);

        Assert.True(context.FinderUsed);
    }

    public class Context : ScenarioContext
    {
        public bool FinderUsed { get; set; }
        public bool HandledOtherMessage { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class CustomFinder : IFindSagas<TestSaga.SagaData>.Using<StartSagaMessage>
        {
            // ReSharper disable once MemberCanBePrivate.Global
            public Context Context { get; set; }

            public Task<TestSaga.SagaData> FindBy(StartSagaMessage message, SynchronizedStorageSession session, ReadOnlyContextBag context)
            {
                Context.FinderUsed = true;

                return session.GetSagaData<TestSaga.SagaData>(
                    context: context,
                    whereClause: "json_value(Data,'$.Property') = @propertyValue",
                    appendParameters: (builder, append) =>
                    {
                        var parameter = builder();
                        parameter.ParameterName = "propertyValue";
                        parameter.Value = "Test";
                        append(parameter);
                    });
            }
        }

        public class TestSaga : SqlSaga<TestSaga.SagaData>,
            IAmStartedByMessages<StartSagaMessage>
        {
            public Context TestContext { get; set; }

            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                TestContext.HandledOtherMessage = true;
                return Task.FromResult(0);
            }

            protected override string CorrelationPropertyName => null;

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