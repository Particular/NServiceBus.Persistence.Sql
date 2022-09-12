namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Features;
    using ObjectBuilder;

    class CaptureBuilderFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) =>
            context.RegisterStartupTask(b => new CaptureBuilderStartupTask(b, b.Build<ScenarioContext>()));

        class CaptureBuilderStartupTask : FeatureStartupTask
        {
            public CaptureBuilderStartupTask(IBuilder serviceProvider, ScenarioContext context)
            {
                if (context is IInjectBuilder c)
                {
                    c.Builder = serviceProvider;
                }
            }

            protected override Task OnStart(IMessageSession session) => Task.CompletedTask;

            protected override Task OnStop(IMessageSession session) => Task.CompletedTask;
        }
    }
}