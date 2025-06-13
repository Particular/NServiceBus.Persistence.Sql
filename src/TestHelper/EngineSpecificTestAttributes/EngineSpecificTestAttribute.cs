using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public abstract class EngineSpecificTestAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionStringName);
        context.OutWriter.WriteLine($"Found {ConnectionStringName} connection string with value {connection ?? ""}.");        
        if (string.IsNullOrWhiteSpace(connection))
        {
            context.OutWriter.WriteLine($"Ignoring because environment variable {ConnectionStringName} not available.");
            Assert.Ignore($"Ignoring because environment variable {ConnectionStringName} not available.");
        }
    }

    protected abstract string ConnectionStringName { get; }
}