﻿using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
class InnerTaskTests
{
    [Test]
    public void IntegrationTest()
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
            logError: (error, type) => throw new Exception(error));
        innerTask.Execute();
        var files = Directory.EnumerateFiles(temp, "*.*", SearchOption.AllDirectories)
                        .Select(s => s.Replace(temp, "temp").ConvertPathSeparators("/"))
                        .OrderBy(f => f) // Deterministic order
                        .ToList();
        Assert.That(files, Is.Not.Empty);

        Approver.Verify(files);
    }
}