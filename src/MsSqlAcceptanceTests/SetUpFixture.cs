using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        MsSqlConnectionBuilder.DropDbIfCollationIncorrect();
        MsSqlConnectionBuilder.CreateDbIfNotExists();
    }
}