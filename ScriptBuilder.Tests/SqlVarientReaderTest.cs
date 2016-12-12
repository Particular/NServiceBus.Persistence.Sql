using System.IO;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SqlVarientReaderTest
{

    [Test]
    public void Simple()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var assemblyResolver = new DefaultAssemblyResolver();
        assemblyResolver.AddSearchDirectory(TestContext.CurrentContext.TestDirectory);
        var readerParameters = new ReaderParameters(ReadingMode.Deferred)
        {
            AssemblyResolver = assemblyResolver
        };
        var module = ModuleDefinition.ReadModule(path, readerParameters);
        ObjectApprover.VerifyWithJson(SqlVarientReader.Read(module).ToList());
    }

}