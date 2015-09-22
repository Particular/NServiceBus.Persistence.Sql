using NServiceBus;
using NServiceBus.Features;

class SagaFeature : Feature
{
    SagaFeature()
    {
        DependsOn<Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
    }
}