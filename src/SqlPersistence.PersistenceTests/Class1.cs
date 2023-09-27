﻿namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_persisting_a_saga_with_an_escalated_DTC_transaction2 : SagaPersisterTests
    {
        [Test]
        public async Task Save_should_fail_when_data_changes_between_concurrent_instances()
        {
            configuration.RequiresDtcSupport();

            var persister = configuration.SagaStorage;
            var sagaData = new TestSagaData { SomeId = Guid.NewGuid().ToString() };
            await SaveSaga(sagaData);
            var generatedSagaId = sagaData.Id;

            var dtcTransactionSource = new TaskCompletionSource();
            var unenlistedUpdateSource = new TaskCompletionSource();
            var enlistedUpdateSource = new TaskCompletionSource();

            var enlistmentNotifier = new EnlistmentWhichEnforcesDtcEscalation(dtcTransactionSource);

            var unenlistedOperation = Task.Run(async () =>
            {
                using var unenlistedSession = configuration.CreateStorageSession();
                var unenlistedContextBag = configuration.GetContextBagForSagaStorage();

                await unenlistedSession.Open(unenlistedContextBag);
                var unenlistedSagaRecord = await persister.Get<TestSagaData>(generatedSagaId, unenlistedSession, unenlistedContextBag);

                await enlistedUpdateSource.Task;

                unenlistedSagaRecord.LastUpdatedBy = "Unenlisted";
                await persister.Update(unenlistedSagaRecord, unenlistedSession, unenlistedContextBag);
                await unenlistedSession.CompleteAsync();

                unenlistedUpdateSource.SetResult();
            });

            var enlistedOperation = Task.Run(() => Assert.That(async () =>
            {
                using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                Transaction.Current.EnlistDurable(EnlistmentWhichEnforcesDtcEscalation.Id, enlistmentNotifier, EnlistmentOptions.None);

                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                using var enlistedSession = configuration.CreateStorageSession();
                var enlistedContextBag = configuration.GetContextBagForSagaStorage();

                await enlistedSession.TryOpen(transportTransaction, enlistedContextBag);
                var enlistedSagaRecord = await persister.Get<TestSagaData>(generatedSagaId, enlistedSession, enlistedContextBag);

                enlistedUpdateSource.SetResult();

                await unenlistedUpdateSource.Task;

                enlistedSagaRecord.LastUpdatedBy = "Enlisted";
                await persister.Update(enlistedSagaRecord, enlistedSession, enlistedContextBag);
                await enlistedSession.CompleteAsync();

                tx.Complete();
            }, Throws.Exception));

            await Task.WhenAll(dtcTransactionSource.Task, unenlistedOperation, enlistedOperation).WaitAsync(TimeSpan.FromSeconds(30));

            var updatedSagaData = await GetById<TestSagaData>(generatedSagaId);

            Assert.IsTrue(enlistmentNotifier.RollbackWasCalled);
            Assert.IsFalse(enlistmentNotifier.CommitWasCalled);
            Assert.AreEqual("Unenlisted", updatedSagaData.LastUpdatedBy);
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

        class EnlistmentWhichEnforcesDtcEscalation(TaskCompletionSource source) : IEnlistmentNotification
        {
            public bool RollbackWasCalled { get; private set; }

            public bool CommitWasCalled { get; private set; }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                CommitWasCalled = true;
                source.SetResult();
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                RollbackWasCalled = true;
                source.SetResult();
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                source.SetResult();
                enlistment.Done();
            }

            public static readonly Guid Id = Guid.NewGuid();
        }

        public When_persisting_a_saga_with_an_escalated_DTC_transaction2(TestVariant param) : base(param)
        {
        }
    }
}