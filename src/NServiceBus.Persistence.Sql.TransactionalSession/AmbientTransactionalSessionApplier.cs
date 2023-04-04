namespace NServiceBus.TransactionalSession;

using System;
using System.Transactions;

class AmbientTransactionalSessionApplier : OpenSessionOptionCustomization
{
    CommittableTransaction committableTransaction;

    public override void ApplyBeforeOpen(OpenSessionOptions options)
    {
        //TODO: get options from the configuration
        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(60) // TimeSpan.Zero is default of `TransactionOptions.Timeout`
        };

        committableTransaction = new CommittableTransaction(transactionOptions);

        if (options is SqlPersistenceOpenSessionOptions sqlOptions)
        {
            sqlOptions.SetTransaction(committableTransaction);
        }
    }

    public override void ApplyAfterOpen(OpenSessionOptions options) => Transaction.Current = committableTransaction;
}