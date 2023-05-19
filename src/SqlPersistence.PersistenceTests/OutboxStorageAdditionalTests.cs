namespace NServiceBus.PersistenceTesting.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using NUnit.Framework;
    using Transport;
    using TransportOperation = NServiceBus.Outbox.TransportOperation;

    [TestFixtureSource(typeof(PersistenceTestsConfiguration), nameof(PersistenceTestsConfiguration.OutboxVariants))]
    class OutboxStorageAdditionalTests
    {
        public OutboxStorageAdditionalTests(TestVariant param)
        {
            this.param = param.DeepCopy();
            variant = (SqlTestVariant)param.Values[0];
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration(param);
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        [Test]
        public async Task Should_()
        {
            configuration.RequiresOutboxSupport();
            variant.RequiresOutboxPessimisticConcurrencySupport();

            var messageId = Guid.NewGuid().ToString();
            var storage = configuration.OutboxStorage;

            var firstSessionBeginDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            ContextBag GetContextBagForOutbox(string incomingMessageId)
            {
                var contextBag = new ContextBag();
                contextBag.Set(new IncomingMessage(incomingMessageId, new Dictionary<string, string>(), Array.Empty<byte>()));
                return contextBag;
            };

            async Task FirstSession()
            {
                var firstSessionContextBag = GetContextBagForOutbox(messageId);
                var outboxMessage = await storage.Get(messageId, firstSessionContextBag);
                Assert.Null(outboxMessage);

                Console.WriteLine("First session begin transaction");
                using var transactionA = await storage.BeginTransaction(firstSessionContextBag);
                firstSessionBeginDone.SetResult(true);
                Console.WriteLine("First session began transaction");

                Console.WriteLine("First session store");
                await storage.Store(new OutboxMessage(messageId, Array.Empty<TransportOperation>()), transactionA, firstSessionContextBag);
                Console.WriteLine("First session stored");
                Console.WriteLine("First session commit");

                await Task.Delay(TimeSpan.FromMinutes(2));
                
                await transactionA.Commit();
                Console.WriteLine("First session committed");
            }

            async Task SecondSession()
            {
                var secondSessionContextBag = GetContextBagForOutbox(messageId);
                await firstSessionBeginDone.Task;
                var outboxMessage = await storage.Get(messageId, secondSessionContextBag);
                Assert.Null(outboxMessage);

                Console.WriteLine("Second session begin transaction");
                using var transactionA = await storage.BeginTransaction(secondSessionContextBag);
                Console.WriteLine("Second session began transaction");

                Console.WriteLine("Second session store");
                await storage.Store(new OutboxMessage(messageId, Array.Empty<TransportOperation>()), transactionA, secondSessionContextBag);
                Console.WriteLine("Second session stored");
                Console.WriteLine("Second session commit");
                await transactionA.Commit();
                Console.WriteLine("Second session committed");
            }

            await Task.WhenAll(SecondSession(), FirstSession());

            var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());

            Assert.That(message, Is.Not.Null);
            CollectionAssert.IsEmpty(message.TransportOperations);
        }

        IPersistenceTestsConfiguration configuration;
        TestVariant param;
        readonly SqlTestVariant variant;
    }
}