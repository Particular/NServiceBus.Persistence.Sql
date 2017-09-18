using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
#if NET452
using ObjectApproval;
#endif

[TestFixture]
class InnerTaskTests
{
    [Test]
    public void IntegrationTest()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var temp = Path.Combine(testDirectory, "InnerTaskTemp");
        if (!Directory.Exists(temp))
        {
            return;
        }
        Directory.Delete(temp, true);
        var assemblyPath = Path.Combine(testDirectory, "ScriptBuilderTask.Tests.Target.dll");
        var intermediatePath = Path.Combine(temp, "IntermediatePath");
        var promotePath = Path.Combine(temp, "PromotePath");
        Directory.CreateDirectory(temp);
        Directory.CreateDirectory(intermediatePath);

        Action<string, string> logError = (error, s1) =>
        {
            throw new Exception(error);
        };
        var innerTask = new InnerTask(assemblyPath, intermediatePath, "TheProjectDir", promotePath, logError);
        innerTask.Execute();
        var files = Directory.EnumerateFiles(temp, "*.*", SearchOption.AllDirectories).Select(s => s.Replace(temp, "temp")).ToList();
        Assert.IsNotEmpty(files);

#if NET452
        ObjectApprover.VerifyWithJson(files);
#endif
    }
}