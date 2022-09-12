namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Features;

    public class CaptureServiceProviderStartupTask : FeatureStartupTask
    {
        public CaptureServiceProviderStartupTask(IServiceProvider serviceProvider, ScenarioContext context)
        {
            if (context is IInjectServiceProvider c)
            {
                c.ServiceProvider = serviceProvider;
            }
        }

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}