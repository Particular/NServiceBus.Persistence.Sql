using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
class InnerTaskTests
{
    [Test]
    public void When_assembly_is_not_isolated_should_succeed_without_reference_paths()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var temp = Path.Combine(testDirectory, "InnerTaskTemp");
        if (Directory.Exists(temp))
        {
            Directory.Delete(temp, true);
        }

        var intermediatePath = Path.Combine(temp, "IntermediatePath");
        Directory.CreateDirectory(temp);
        Directory.CreateDirectory(intermediatePath);

        var innerTask = new InnerTask(
            assemblyPath: Path.Combine(testDirectory, "ScriptBuilderTask.Tests.Target.dll"),
            intermediateDirectory: intermediatePath,
            projectDirectory: "TheProjectDir",
            solutionDirectory: Path.Combine(temp, "PromotePath"),
            referencePaths: [],
            logError: (error, type) => throw new Exception(error));
        innerTask.Execute();
        var files = Directory.EnumerateFiles(temp, "*.*", SearchOption.AllDirectories)
                        .Select(s => s.Replace(temp, "temp").ConvertPathSeparators("/"))
                        .OrderBy(f => f) // Deterministic order
                        .ToList();
        Assert.That(files, Is.Not.Empty);

        Approver.Verify(files);
    }

    [Test]
    public void When_assembly_isolated_without_dependencies_should_fail_without_reference_paths()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var temp = Path.Combine(testDirectory, "IsolatedAssemblyTest");
        if (Directory.Exists(temp))
        {
            Directory.Delete(temp, true);
        }

        // Create an isolated folder with only the target assembly (no dependencies)
        var isolatedAssemblyPath = Path.Combine(temp, "Isolated");
        var intermediatePath = Path.Combine(temp, "IntermediatePath");
        Directory.CreateDirectory(isolatedAssemblyPath);
        Directory.CreateDirectory(intermediatePath);

        var sourceAssembly = Path.Combine(testDirectory, "ScriptBuilderTask.Tests.Target.dll");
        var isolatedAssembly = Path.Combine(isolatedAssemblyPath, "ScriptBuilderTask.Tests.Target.dll");
        File.Copy(sourceAssembly, isolatedAssembly);

        var innerTask = new InnerTask(
            assemblyPath: isolatedAssembly,
            intermediateDirectory: intermediatePath,
            projectDirectory: "TheProjectDir",
            solutionDirectory: Path.Combine(temp, "PromotePath"),
            referencePaths: [], // Without reference paths, should fail because NServiceBus.Persistence.Sql.dll is not in the isolated folder
            logError: static (_, _) => { });

        Assert.That(() => innerTask.Execute(), Throws.TypeOf<FileNotFoundException>().And.Message.Contains("NServiceBus.Persistence.Sql"));
    }

    [Test]
    public void When_assembly_isolated_without_dependencies_should_succeed_with_reference_paths()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var temp = Path.Combine(testDirectory, "IsolatedAssemblyTestWithRefs");
        if (Directory.Exists(temp))
        {
            Directory.Delete(temp, true);
        }

        // Create an isolated folder with only the target assembly (no dependencies)
        var isolatedAssemblyPath = Path.Combine(temp, "Isolated");
        var intermediatePath = Path.Combine(temp, "IntermediatePath");
        Directory.CreateDirectory(isolatedAssemblyPath);
        Directory.CreateDirectory(intermediatePath);

        var sourceAssembly = Path.Combine(testDirectory, "ScriptBuilderTask.Tests.Target.dll");
        var isolatedAssembly = Path.Combine(isolatedAssemblyPath, "ScriptBuilderTask.Tests.Target.dll");
        File.Copy(sourceAssembly, isolatedAssembly);

        // With reference paths pointing to the actual dependency location, should succeed
        string[] referencePaths = [Path.Combine(testDirectory, "NServiceBus.Persistence.Sql.dll")];

        var innerTask = new InnerTask(
            assemblyPath: isolatedAssembly,
            intermediateDirectory: intermediatePath,
            projectDirectory: "TheProjectDir",
            solutionDirectory: Path.Combine(temp, "PromotePath"),
            referencePaths: referencePaths,
            logError: static (error, _) => throw new Exception(error));

        Assert.DoesNotThrow(() => innerTask.Execute());

        var files = Directory.EnumerateFiles(intermediatePath, "*.sql", SearchOption.AllDirectories);
        Assert.That(files, Is.Not.Empty, "Should have generated SQL scripts");
    }
}