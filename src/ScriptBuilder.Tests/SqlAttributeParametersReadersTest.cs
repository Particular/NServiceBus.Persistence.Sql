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
        ObjectApprover.VerifyWithJson(SqlVariantReader.Read(module).ToList());
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
        var tryRead = ScriptPromotionPathReader.TryRead(module, out path);
        Assert.IsTrue(tryRead);
        ObjectApprover.VerifyWithJson(path);
    }
}