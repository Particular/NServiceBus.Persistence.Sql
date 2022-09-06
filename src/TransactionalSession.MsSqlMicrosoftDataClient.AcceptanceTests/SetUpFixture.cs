using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        MsSqlMicrosoftDataClientConnectionBuilder.DropDbIfCollationIncorrect(true);
        MsSqlMicrosoftDataClientConnectionBuilder.CreateDbIfNotExists(true);
    }
}