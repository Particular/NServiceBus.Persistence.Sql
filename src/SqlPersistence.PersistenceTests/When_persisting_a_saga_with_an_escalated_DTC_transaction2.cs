namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_persisting_a_saga_with_an_escalated_DTC_transaction2 : SagaPersisterTests
    {
        [Test]
        public async Task Should_rollback_when_dtc_transaction_is_aborted()
        {
            configuration.RequiresDtcSupport();

            var persister = configuration.SagaStorage;
            var sagaData = new TestSagaData { SomeId = Guid.NewGuid().ToString(), LastUpdatedBy = "Unchanged" };
            await SaveSaga(sagaData);
            var generatedSagaId = sagaData.Id;

            var enlistmentNotifier = new EnlistmentNotifier(abortTransaction: true);

            Assert.That(async () =>
            {
                using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                Transaction.Current.EnlistDurable(EnlistmentNotifier.Id, enlistmentNotifier, EnlistmentOptions.None);

                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                using var enlistedSession = configuration.CreateStorageSession();
                var enlistedContextBag = configuration.GetContextBagForSagaStorage();

                await enlistedSession.TryOpen(transportTransaction, enlistedContextBag);
                var enlistedSagaRecord = await persister.Get<TestSagaData>(generatedSagaId, enlistedSession, enlistedContextBag);

                enlistedSagaRecord.LastUpdatedBy = "Changed";
                await persister.Update(enlistedSagaRecord, enlistedSession, enlistedContextBag);

                await enlistedSession.CompleteAsync();

                tx.Complete();
            }, Throws.Exception.TypeOf<TransactionAbortedException>());

            await enlistmentNotifier.CompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(30));

            var updatedSagaData = await GetById<TestSagaData>(generatedSagaId);

            Assert.IsTrue(enlistmentNotifier.RollbackWasCalled);
            Assert.IsFalse(enlistmentNotifier.CommitWasCalled);
            Assert.AreEqual("Unchanged", updatedSagaData.LastUpdatedBy);
        }

        [Test]
        public async Task Should_rollback_dtc_transaction_when_storage_session_rolls_back()
        {
            configuration.RequiresDtcSupport();

            var sagaData = new TestSagaData
            {
                SomeId = Guid.NewGuid().ToString(),
                LastUpdatedBy = "Unchanged"
            };
            await SaveSaga(sagaData);

            var enlistmentNotifier = new EnlistmentNotifier(abortTransaction: false);

            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Transaction.Current.EnlistDurable(EnlistmentNotifier.Id, enlistmentNotifier, EnlistmentOptions.None);

                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                var contextBag = configuration.GetContextBagForSagaStorage();
                using (var session = configuration.CreateStorageSession())
                {
                    await session.TryOpen(transportTransaction, contextBag);

                    var sagaFromStorage = await configuration.SagaStorage.Get<TestSagaData>(sagaData.Id, session, contextBag);
                    sagaFromStorage.LastUpdatedBy = "Changed";

                    await configuration.SagaStorage.Update(sagaFromStorage, session, contextBag);

                    // Do not complete
                }
            }

            await enlistmentNotifier.CompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(30));

            var hopefullyNotUpdatedSaga = await GetById<TestSagaData>(sagaData.Id);

            Assert.NotNull(hopefullyNotUpdatedSaga);
            Assert.AreEqual("Unchanged", hopefullyNotUpdatedSaga.LastUpdatedBy);
            Assert.IsFalse(enlistmentNotifier.CommitWasCalled);
            Assert.IsTrue(enlistmentNotifier.RollbackWasCalled);
        }

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
            }
        }

        public class TestSagaData : ContainSagaData
        {
            public string SomeId { get; set; }

            public string LastUpdatedBy { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        class EnlistmentNotifier(bool abortTransaction) : IEnlistmentNotification
        {
            public TaskCompletionSource CompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool RollbackWasCalled { get; private set; }

            public bool CommitWasCalled { get; private set; }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                if (!abortTransaction)
                {
                    preparingEnlistment.Prepared();
                }

                // Remain un-prepared so the transaction aborts.
            }

            public void Commit(Enlistment enlistment)
            {
                CommitWasCalled = true;
                CompletionSource.SetResult();
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                RollbackWasCalled = true;
                CompletionSource.SetResult();
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                CompletionSource.SetResult();
                enlistment.Done();
            }

            public static readonly Guid Id = Guid.NewGuid();
        }

        public When_persisting_a_saga_with_an_escalated_DTC_transaction2(TestVariant param) : base(param)
        {
        }
    }
}