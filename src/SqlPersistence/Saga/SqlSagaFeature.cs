using NServiceBus.Features;

class SqlSagaFeature : Feature
{
    SqlSagaFeature()
    {
        DependsOn<Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
    }

}