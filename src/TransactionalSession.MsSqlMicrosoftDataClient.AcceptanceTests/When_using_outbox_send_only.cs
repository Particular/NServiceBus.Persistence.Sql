namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using Configuration.AdvancedExtensibility;
using NUnit.Framework;
using Pipeline;

public class When_using_outbox_send_only : NServiceBusAcceptanceTest
{
    [Test()]
    public async Task Should_send_messages_on_transactional_session_commit()
    {
        await OutboxHelpers.CreateOutboxTable<SendOnlyEndpoint>();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendOnlyEndpoint>(s => s.When(async (_, ctx) =>
            {
                using var scope = ctx.ServiceProvider.CreateScope();
                using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                await transactionalSession.Open(new SqlPersistenceOpenSessionOptions());

                var options = new SendOptions();

                options.SetDestination(Conventions.EndpointNamingConvention.Invoke(typeof(AnotherEndpoint)));

                await transactionalSession.Send(new SampleMessage(), options);

                await transactionalSession.Commit(CancellationToken.None);
            }))
            .WithEndpoint<AnotherEndpoint>()
            .WithEndpoint<ProcessorEndpoint>()
            .Done(c => c.MessageReceived)
            .Run();

        Assert.That(context.ControlMessageReceived, Is.True);
        Assert.That(context.MessageReceived, Is.True);
    }

    class Context : ScenarioContext, IInjectServiceProvider
    {
        public bool MessageReceived { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public bool ControlMessageReceived { get; set; }
    }

    class SendOnlyEndpoint : EndpointConfigurationBuilder
    {
        public SendOnlyEndpoint() => EndpointSetup<TransactionSessionWithOutboxEndpoint>(c =>
        {
            c.GetSettings().Set(TransactionSessionDefaultServer.TransactionalSessionOptionsKey, new TransactionalSessionOptions { ProcessorAddress = Conventions.EndpointNamingConvention.Invoke(typeof(ProcessorEndpoint)) });

            c.SendOnly();
        });
    }

    class AnotherEndpoint : EndpointConfigurationBuilder, IDoNotCaptureServiceProvider
    {
        public AnotherEndpoint() => EndpointSetup<TransactionSessionDefaultServer>();

        class SampleHandler(Context testContext) : IHandleMessages<SampleMessage>
        {
            public Task Handle(SampleMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                return Task.CompletedTask;
            }
        }
    }

    class ProcessorEndpoint : EndpointConfigurationBuilder, IDoNotCaptureServiceProvider
    {
        public ProcessorEndpoint() => EndpointSetup<TransactionSessionWithOutboxEndpoint>(c =>
        {
            var sendOnlyEndpointName = Conventions.EndpointNamingConvention.Invoke(typeof(SendOnlyEndpoint));
            var tablePrefix = $"{TestTableNameCleaner.Clean(sendOnlyEndpointName)}_";

            // use the outbox table of the send only endpoint
            c.GetSettings().Get<PersistenceExtensions<SqlPersistence>>().TablePrefix(tablePrefix);

            c.Pipeline.Register(typeof(DiscoverControlMessagesBehavior), "Discovers control messages");
        });

        class DiscoverControlMessagesBehavior(Context testContext) : Behavior<ITransportReceiveContext>
        {
            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                if (context.Message.Headers.ContainsKey("NServiceBus.TransactionalSession.CommitDelayIncrement"))
                {
                    testContext.ControlMessageReceived = true;
                }

                await next();
            }
        }
    }

    class SampleMessage : ICommand
    {
    }
}