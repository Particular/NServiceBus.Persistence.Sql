namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests;

    public class When_headers_contain_special_characters : NServiceBusAcceptanceTest
    {
        //static Dictionary<string, string> sentHeaders = new Dictionary<string, string>
        //{
        //    { "a-B1", "a-B" },
        //    { "a-B2", "a-ɤϡ֎ᾣ♥-b" },
        //    { "a-ɤϡ֎ᾣ♥-B3", "a-B" },
        //    { "a-B4", "a-\U0001F60D-b" },
        //    { "a-\U0001F605-B5", "a-B" },
        //    { "a-B6", "a-😍-b" },
        //    { "a-😅-B7", "a-B" },
        //    {"a-b8", "奥曼克"},
        //    {"a-B9", "٩(-̮̮̃-̃)۶ ٩(●̮̮̃•̃)۶ ٩(͡๏̯͡๏)۶ ٩(-̮̮̃•̃)" },
        //    {"a-b10", "தமிழ்" }
        //};

        [TestCase("a-ɤϡ֎ᾣ♥-b")]
        [TestCase("a-ɤϡ֎ᾣ♥-B3")]
        [TestCase("a-\U0001F60D-b")]
        [TestCase("a-\U0001F605-B5")]
        [TestCase("a-😍-b")]
        [TestCase("奥曼克")]
        [TestCase("٩(-̮̮̃-̃)۶ ٩(●̮̮̃•̃)۶ ٩(͡๏̯͡๏)۶ ٩(-̮̮̃•̃)")]
        [TestCase("a-😅-B7")]
        [TestCase("தமிழ்")]
        public async Task Outbox_should_work(string header)
        {
            var context =
                await Scenario.Define<Context>()
                    .WithEndpoint<OutboxEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder()
                    {
                        Header = header
                    })))
                    .Done(c => c.MessageReceived)
                    .Run();

            Assert.IsNotEmpty(context.UnicodeHeaders);
            var testheader = new[]{ new KeyValuePair<string, string>(header, header)};
            CollectionAssert.IsSubsetOf(testheader, context.UnicodeHeaders);
        }

        class Context : ScenarioContext
        {
            public IReadOnlyDictionary<string, string> UnicodeHeaders { get; set; }
            public bool MessageReceived { get; set; }
            public bool SavedOutBoxRecord { get; set; }
        }

        public class OutboxEndpoint : EndpointConfigurationBuilder
        {
            public OutboxEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableOutbox();
                    b.GetSettings().Set("DisableOutboxTransportCheck", true);
                    b.Pipeline.Register("BlowUpBeforeDispatchBehavior", new BlowUpBeforeDispatchBehavior((Context)ScenarioContext), "Force reading the message from Outbox storage.");
                    b.Recoverability().Immediate(a => a.NumberOfRetries(1));
                });
            }
            class BlowUpBeforeDispatchBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
            {
                Context _context;
                public BlowUpBeforeDispatchBehavior(Context context)
                {
                    _context = context;
                }
                public async Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
                {
                    if (!_context.SavedOutBoxRecord)
                    {
                        _context.SavedOutBoxRecord = true;
                        throw new Exception();
                    }

                    await next(context).ConfigureAwait(false);
                }
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    var sendOrderAcknowledgement = new SendOrderAcknowledgement();
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    
                        sendOptions.SetHeader(message.Header, message.Header);
                    
                    return context.Send(sendOrderAcknowledgement, sendOptions);
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    Context.MessageReceived = true;
                    Context.UnicodeHeaders = context.MessageHeaders;
                    return Task.FromResult(0);
                }
            }
        }

        public class PlaceOrder : ICommand
        {
            public string Header { get; set; }
        }

        public class SendOrderAcknowledgement : IMessage
        {
        }
    }
}