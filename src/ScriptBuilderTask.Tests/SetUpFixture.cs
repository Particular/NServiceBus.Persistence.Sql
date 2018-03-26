using System;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
#if NET452
        ObjectApproval.ObjectApprover.JsonSerializer.DefaultValueHandling = DefaultValueHandling.Include;
#endif
        FixCurrentDirectory();
    }

    void FixCurrentDirectory([CallerFilePath] string callerFilePath="")
    {
        Environment.CurrentDirectory = Directory.GetParent(callerFilePath).FullName;
    }
}