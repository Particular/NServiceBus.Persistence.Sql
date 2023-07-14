namespace AuroraSetup
{
    using Amazon.CDK;
    using Constructs;

    public class AuroraTestInfrastructure : Stack
    {
        internal AuroraTestInfrastructure(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // The code that defines your stack goes here
        }
    }
}