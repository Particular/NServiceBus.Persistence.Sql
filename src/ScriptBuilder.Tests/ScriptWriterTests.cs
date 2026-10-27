using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
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
        using var module = ModuleDefinition.ReadModule(typeof(ScriptWriterTests).Assembly.Location);
        var sagaFixture = module.Types.Single(x => x.Name == nameof(SagaDefinitionReaderTest));
        foreach (var type in sagaFixture.NestedTypes.Where(x => x.Name != nameof(SagaDefinitionReaderTest.SimpleSaga)).ToList())
        {
            sagaFixture.NestedTypes.Remove(type);
        }
        module.Types.Clear();
        module.Types.Add(sagaFixture);
        var writer = new SagaWriter(clean: true, overwrite: true, relativePath, module, (_, _) => { });

        writer.WriteScripts(BuildSqlDialect.MsSqlServer);

        Assert.Multiple(() =>
        {
            Assert.That(Path.Combine(relativePath, "Sagas", "SimpleSaga_Create.sql"), Does.Exist);
            Assert.That(Path.Combine(relativePath, "Sagas", "SimpleSaga_Drop.sql"), Does.Exist);
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
