using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        TestApprover.JsonSerializer.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include;
    }
}