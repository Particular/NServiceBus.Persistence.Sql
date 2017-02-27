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

        var path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
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
    public void Path()
    {
        var path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var assemblyResolver = new DefaultAssemblyResolver();
        assemblyResolver.AddSearchDirectory(TestContext.CurrentContext.TestDirectory);
        var readerParameters = new ReaderParameters(ReadingMode.Deferred)
        {
            AssemblyResolver = assemblyResolver
        };
        var module = ModuleDefinition.ReadModule(path, readerParameters);
        var buildSqlVariants = OutputPathReader.Read(module);
        ObjectApprover.VerifyWithJson(buildSqlVariants);
    }
}
