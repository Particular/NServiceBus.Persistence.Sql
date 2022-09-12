namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Linq;
    using System.Threading;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    /// <summary>
    ///     Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]
    public abstract class NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetUp()
        {
#if NETFRAMEWORK
            // Hack: prevents SerializationException ... Type 'x' in assembly 'y' is not marked as serializable.
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-deserialization-of-objects-across-app-domains
            System.Configuration.ConfigurationManager.GetSection("X");
#endif
            Conventions.EndpointNamingConvention = t =>
            {
                string classAndEndpoint = t.FullName.Split('.').Last();

                string testName = classAndEndpoint.Split('+').First();

                testName = testName.Replace("When_", "");

                string endpointBuilder = classAndEndpoint.Split('+').Last();

                testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

                testName = testName.Replace("_", "");

                return testName + "." + endpointBuilder;
            };
        }
    }
}