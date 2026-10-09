using System;
using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class ScriptWriterTests
{
    string relativePath;

    [SetUp]
    public void SetUp()
    {
        relativePath = Path.Combine(nameof(ScriptWriterTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(relativePath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(relativePath))
        {
            Directory.Delete(relativePath, true);
        }
    }

    [Test]
    public void Saga_scripts_are_written_to_a_relative_path()
    {
        var saga = new SagaDefinition(
            tableSuffix: "OrderSaga",
            name: "OrderSaga",
            correlationProperty: new CorrelationProperty("OrderId", CorrelationPropertyType.Guid));
        var writer = new SagaWriter(clean: true, overwrite: true, relativePath, [saga]);

        writer.WriteScripts(BuildSqlDialect.MsSqlServer);

        Assert.Multiple(() =>
        {
            Assert.That(Path.Combine(relativePath, "Sagas", "OrderSaga_Create.sql"), Does.Exist);
            Assert.That(Path.Combine(relativePath, "Sagas", "OrderSaga_Drop.sql"), Does.Exist);
        });
    }

    [Test]
    public void Clean_removes_an_existing_script_before_writing()
    {
        new OutboxWriter(clean: true, overwrite: false, relativePath).WriteScripts(BuildSqlDialect.MsSqlServer);

        // overwrite is false, so this only succeeds if clean deleted the scripts written above
        Assert.DoesNotThrow(() => new OutboxWriter(clean: true, overwrite: false, relativePath).WriteScripts(BuildSqlDialect.MsSqlServer));
    }
}
