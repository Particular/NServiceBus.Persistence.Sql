using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using NUnit.Framework;


[TestFixture]
public class GetBaseTypeTest
{
    ModuleDefinition module;

    public GetBaseTypeTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilderTask.Tests.dll");
        module = ModuleDefinition.ReadModule(path);
    }

    [Test]
    public void SimpleTypeTest()
    {
        var type = module.GetTypeDefinition<SimpleType>();
        Assert.AreEqual("Object", type.GetBase().DisplayName());
    }

    public class SimpleType
    {
    }

    [Test]
    public void GenericTypeTest()
    {
        var type = module.GetTypeDefinition<GenericType>();
        Assert.AreEqual("Dictionary<String, Int32>", type.GetBase().DisplayName());
    }

    public class GenericType : Dictionary<string, int>
    {
    }
    [Test]
    public void HierarchyTest()
    {
        var type = module.GetTypeDefinition<Child>().GetBase();
        Assert.AreEqual("Generic3<Int32, String, Boolean>", type.DisplayName());
        type = type.GetBase();
        Assert.AreEqual("Generic2<String, Int32>", type.DisplayName());
        type = type.GetBase();
        Assert.AreEqual("Generic1<Int32>", type.DisplayName());
    }

    public class Generic1<T1>
    {
    }

    public class Generic2<T2, T3>: Generic1<T3>
    {
    }

    public class Generic3<T4, T5, T6> : Generic2<T5, T4>
    {
    }
    public class Child : Generic3<int, string, bool>
    {
    }
}