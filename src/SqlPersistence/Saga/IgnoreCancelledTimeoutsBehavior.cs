using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

class IgnoreCancelledTimeoutsBehavior : Behavior<IInvokeHandlerContext>
{
    static Task<int> SkipCancelledTimeout = Task.FromResult(0);

    public override Task Invoke(IInvokeHandlerContext context, Func<Task> next)
    {
        if (!context.Headers.TryGetValue("NServiceBus.Sql.TimeoutId", out string timeoutId))
        {
            return next();
        }
        if (!context.Extensions.TryGet(out SagaInstanceMetadata metadata))
        {
            return next();
        }
        if (!metadata.CanceledTimeouts.Contains(timeoutId))
        {
            return next();
        }
        //Timeout can be safely removed from the collection because DTC or Outbox will prevent delivering it again.
        //In endpoints without DTC/Outbox cancelled timeouts can be delivered even though they are cancelled if a message get duplicated along the way
        metadata.CanceledTimeouts.Remove(timeoutId);
        // ReSharper disable once AssignNullToNotNullAttribute
        if (metadata.PendingTimeouts.TryGetValue(context.MessageMetadata.MessageType.FullName, out var pending))
        {
            pending.Remove(timeoutId);
        }
        return SkipCancelledTimeout;
    }

    public class Registration : RegisterStep
    {
        public Registration()
            : base("IgnoreCancelledTimeoutsBehavior", typeof(IgnoreCancelledTimeoutsBehavior), "Ignores timeouts that have been marked as cancelled")
        {
            InsertAfter("InvokeSaga");
        }
    }
}
