namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_updating_saga : SagaPersisterTests
    {
        [Test]
        public async Task It_should_trim_state_when_storing_smaller_payload()
        {
            // When updating an existing saga where the serialized state is smaller in length than the previous the column value should not have any left over data from the previous value.
            // The deserializer ignores any trailing 

            if (param.Values[0] is not SqlTestVariant sqlVariant)
            {
                Assert.Ignore("Only relevant for SQL Server");
                return; // Satisfy compiler
            }

            var sagaData = new SagaState
            {
                CorrelationProperty = Guid.NewGuid().ToString(),
                Payload = "very long state"
            };

            await SaveSaga(sagaData);

            SagaState retrieved;
            var context = configuration.GetContextBagForSagaStorage();
            var persister = configuration.SagaStorage;

            using (var completeSession = configuration.CreateStorageSession())
            {
                await completeSession.Open(context);

                retrieved = await persister.Get<SagaState>("CorrelationProperty", sagaData.CorrelationProperty, completeSession, context);

                retrieved.Payload = "short";

                await persister.Update(retrieved, completeSession, context);
                await completeSession.CompleteAsync();
            }

            Assert.LessOrEqual(retrieved.Payload, sagaData.Payload); // No real need, but here to prevent accidental updates

            var retrieved2 = await GetById<SagaState>(sagaData.Id);

            Assert.AreEqual(retrieved.Payload, retrieved2.Payload);

            await using var con = sqlVariant.Open();
            await con.OpenAsync();
            var cmd = con.CreateCommand();
            cmd.CommandText = $"SELECT Data FROM [PersistenceTests_TS] WHERE Id = '{retrieved.Id}'";
            var data = (string)await cmd.ExecuteScalarAsync();

            var countClosingBrackets = data.ToCharArray().Count(x => x == '}');

            Assert.AreEqual(1, countClosingBrackets);
        }

        public class TestSaga : Saga<SagaState>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaState> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.CorrelationProperty);
            }
        }

        public class SagaState : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string Payload { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public When_updating_saga(TestVariant param) : base(param)
        {
        }
    }
}