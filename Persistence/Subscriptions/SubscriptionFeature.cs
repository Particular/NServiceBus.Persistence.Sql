using NServiceBus.Features;

class SubscriptionFeature : Feature
{
    SubscriptionFeature()
    {
        DependsOn<StorageDrivenPublishing>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        //context.Container.ConfigureComponent<SubscriptionInstaller>(DependencyLifecycle.InstancePerCall);
    }
}