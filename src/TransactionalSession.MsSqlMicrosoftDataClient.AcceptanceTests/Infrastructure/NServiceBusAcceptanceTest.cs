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