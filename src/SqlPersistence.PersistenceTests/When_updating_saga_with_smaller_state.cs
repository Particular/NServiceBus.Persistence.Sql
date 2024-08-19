namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_updating_saga_with_smaller_state : SagaPersisterTests
    {
        [Test]
        public async Task It_should_truncate_the_stored_state()
        {
            // When updating an existing saga where the serialized state is smaller in length than the previous the column value should not have any left over data from the previous value.
            // The deserializer ignores any trailing 

            var sqlVariant = (SqlTestVariant)param.Values[0];

            if (sqlVariant.Dialect is not SqlDialect.MsSqlServer)
            {
                Assert.Ignore("Only relevant for SQL Server");
                return; // Satisfy compiler
            }

            var sagaData = new SagaWithCorrelationPropertyData
            {
                CorrelatedProperty = Guid.NewGuid().ToString(),
                Payload = "very long state"
            };

            await SaveSaga(sagaData);

            SagaWithCorrelationPropertyData retrieved;
            var context = configuration.GetContextBagForSagaStorage();
            var persister = configuration.SagaStorage;

            using (var completeSession = configuration.CreateStorageSession())
            {
                await completeSession.Open(context);

                retrieved = await persister.Get<SagaWithCorrelationPropertyData>(nameof(sagaData.CorrelatedProperty), sagaData.CorrelatedProperty, completeSession, context);

                retrieved.Payload = "short";

                await persister.Update(retrieved, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var retrieved2 = await GetById<SagaWithCorrelationPropertyData>(sagaData.Id);

            Assert.That(retrieved.Payload, Is.LessThanOrEqualTo(sagaData.Payload)); // No real need, but here to prevent accidental updates
            Assert.That(retrieved2.Payload, Is.EqualTo(retrieved.Payload));

            await using var con = sqlVariant.Open();
            await con.OpenAsync();
            var cmd = con.CreateCommand();
            cmd.CommandText = $"SELECT Data FROM [PersistenceTests_SWCP] WHERE Id = '{retrieved.Id}'";
            var data = (string)await cmd.ExecuteScalarAsync();

            // Payload should only have a single closing bracket, if there are more that means there is trailing data
            var countClosingBrackets = data.ToCharArray().Count(x => x == '}');

            Assert.That(countClosingBrackets, Is.EqualTo(1));
        }

        public class SagaWithCorrelationProperty : Saga<SagaWithCorrelationPropertyData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithCorrelationPropertyData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.CorrelatedProperty);
            }
        }

        public class SagaWithCorrelationPropertyData : ContainSagaData
        {
            public string CorrelatedProperty { get; set; }
            public string Payload { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public When_updating_saga_with_smaller_state(TestVariant param) : base(param)
        {
        }
    }
}