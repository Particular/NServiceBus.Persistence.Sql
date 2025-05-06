namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;

    public class CaptureServiceProviderStartupTask : FeatureStartupTask
    {
        public CaptureServiceProviderStartupTask(IServiceProvider serviceProvider, TransactionalSessionTestContext context, string endpointName) => context.RegisterServiceProvider(serviceProvider, endpointName);

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}