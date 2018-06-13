using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        TestApprover.JsonSerializer.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include;
        FixCurrentDirectory();
    }

    void FixCurrentDirectory([CallerFilePath] string callerFilePath="")
    {
        Environment.CurrentDirectory = Directory.GetParent(callerFilePath).FullName;
    }
}