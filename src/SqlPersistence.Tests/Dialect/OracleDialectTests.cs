using System;
using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class OracleDialectTests
{
    SqlDialect.Oracle dialect;

    [SetUp]
    public void SetUp()
    {
        dialect = new SqlDialect.Oracle();
    }

    [Test]
    public void AcceptsValidTablePrefix() =>
        Assert.DoesNotThrow(() => dialect.ValidateTablePrefix("ThisIsAValidTablePrefix"));

    [Test]
    public void ThrowsForLongTablePrefix() =>
        Assert.Throws<Exception>(
            () => dialect.ValidateTablePrefix("ThisIsATablePrefixThatIsTooLong")
        );

    [Test]
    public void AcceptsLongTablePrefixIfLongTableNamesEnabled()
    {
        dialect.EnableLongTableNames = true;
        Assert.DoesNotThrow(
            () => dialect.ValidateTablePrefix("ThisIsATablePrefixThatIsTooLong")
        );
    }

    [Test]
    public void ThrowsForVeryLongTablePrefixEvenIfLongTableNamesEnabled()
    {
        dialect.EnableLongTableNames = true;
        Assert.Throws<Exception>(
        () => dialect.ValidateTablePrefix("ThisIsATablePrefixWhichIsLongerThan128CharactersBecauseThatIsTheLimitOfTheLatestOracleServerOnceUponAMidnightDrearyWhileIPonderedWeakAndWeary")
        );
    }

    [Test]
    public void AcceptsValidSagaName() =>
        Assert.DoesNotThrow(
            () => dialect.GetSagaTableName(null, "ThisIsAValidSagaName")
        );

    [Test]
    public void ThrowsForLongSagaName() =>
        Assert.Throws<Exception>(
            () => dialect.GetSagaTableName(null, "ThisIsASagaNameWhichIsOver27CharactersAndIsTooLong")
        );

    [Test]
    public void AcceptsLongSagaNameWhenLongTableNamesEnabled()
    {
        dialect.EnableLongTableNames = true;
        Assert.DoesNotThrow(
            () => dialect.GetSagaTableName(null, "ThisIsASagaNameWhichIsOver27CharactersAndIsTooLong")
        );
    }

    [Test]
    public void ThrowsForVeryLongSagaNameEvenIfLongTableNamesEnabled()
    {
        dialect.EnableLongTableNames = true;
        Assert.Throws<Exception>(
            () => dialect.GetSagaTableName(null, "ThisISagaNameWhichIsLongerThan128CharactersBecauseThatIsTheLimitOfTheLatestOracleServerOverManyQuaintAndCuriousVolumeOfForgottenLore")
        );
    }
}