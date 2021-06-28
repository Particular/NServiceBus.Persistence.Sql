using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using OSPlatform = System.Runtime.InteropServices.OSPlatform;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
class OnlyOnWindowsAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        if (!IsOnWindows)
        {
            Assert.Ignore("Only on windows");
        }
    }

    public static bool IsOnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}