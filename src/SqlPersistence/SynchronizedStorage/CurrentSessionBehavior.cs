using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

class CurrentSessionBehavior : Behavior<IIncomingLogicalMessageContext>
{
    readonly CurrentSessionHolder currentSessionHolder;

    public CurrentSessionBehavior(CurrentSessionHolder currentSessionHolder)
    {
        this.currentSessionHolder = currentSessionHolder;
    }

    public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        using (currentSessionHolder.CreateScope())
        {
            await next().ConfigureAwait(false);
        }
    }
}