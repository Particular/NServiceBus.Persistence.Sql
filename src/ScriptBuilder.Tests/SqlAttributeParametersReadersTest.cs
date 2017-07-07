using System.IO;
using System.Linq;
using Mono.Cecil;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SqlAttributeParametersReadersTest
{

    [Test]
    public void Variant()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var assemblyResolver = new DefaultAssemblyResolver();
        assemblyResolver.AddSearchDirectory(TestContext.CurrentContext.TestDirectory);
        var readerParameters = new ReaderParameters(ReadingMode.Deferred)
        {
            AssemblyResolver = assemblyResolver
        };
        var module = ModuleDefinition.ReadModule(path, readerParameters);
        ObjectApprover.VerifyWithJson(SettingsAttributeReader.Read(module).BuildVariants.ToList());
    }

    [Test]
    public void ScriptPromotionPath()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var assemblyResolver = new DefaultAssemblyResolver();
        assemblyResolver.AddSearchDirectory(TestContext.CurrentContext.TestDirectory);
        var readerParameters = new ReaderParameters(ReadingMode.Deferred)
        {
            AssemblyResolver = assemblyResolver
        };
        var module = ModuleDefinition.ReadModule(path, readerParameters);
        var result = SettingsAttributeReader.Read(module);
        ObjectApprover.VerifyWithJson(result.ScriptPromotionPath);
    }
}