﻿using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
[Ignore("Not a supported scenario in v8. Works by accident in v7.")]
public class When_outbox_disabled_and_different_persistence_used_for_sagas : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_enable_synchronized_storage_session()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithDummySaga>(bb => bb.When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.Done)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.Done, Is.True);
            Assert.That(context.SessionCreated, Is.False);
        });
        Assert.That(context.ExceptionMessage, Does.StartWith("Cannot access the SQL synchronized storage session"));
    }

    public class Context : ScenarioContext
    {
        public bool SessionCreated { get; set; }
        public bool Done { get; set; }
        public string ExceptionMessage { get; set; }
    }

    public class EndpointWithDummySaga : EndpointConfigurationBuilder
    {
        public EndpointWithDummySaga()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.UsePersistence<AcceptanceTestingPersistence, StorageType.Sagas>();
            });
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            Context testContext;

            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                try
                {
                    var session = context.SynchronizedStorageSession.SqlPersistenceSession();
                    testContext.SessionCreated = session != null;
                }
                catch (Exception e)
                {
                    testContext.ExceptionMessage = e.Message;
                }
                testContext.Done = true;
                return Task.CompletedTask;
            }
        }

        //This saga is not used but required to activate the saga feature
        public class DummySaga : SqlSaga<DummySagaData>, IAmStartedByMessages<DummyMessage>
        {
            protected override string CorrelationPropertyName => "Dummy";
            protected override void ConfigureMapping(IMessagePropertyMapper mapper)
            {
                mapper.ConfigureMapping<DummyMessage>(m => null);
            }

            public Task Handle(DummyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        public class DummySagaData : ContainSagaData
        {
            public string Dummy { get; set; }
        }
    }

    public class DummyMessage : IMessage
    {
    }

    public class MyMessage : IMessage
    {
    }
}