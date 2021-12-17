using System;
using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class ConventionTests
{
    [Test]
    public void AssertNonPublicTypesHaveNoNamespace()
    {
        foreach (var type in typeof(SqlPersistence).Assembly.GetTypes())
        {
            if (type.IsPublic)
            {
                continue;
            }
            if (type.IsNested)
            {
                continue;
            }
            if (type.Namespace == null)
            {
                continue;
            }
            if (type.BaseType != null && type.BaseType == typeof(Exception))
            {
                continue;
            }
            if (type.Name == "ProcessedByFody")
            {
                continue;
            }
            if (type.Namespace.StartsWith("Microsoft.") || type.Namespace.StartsWith("System."))
            {
                TestContext.WriteLine($"Exception for type: {type.AssemblyQualifiedName}");
                continue;
            }
            throw new Exception($"Type {type.AssemblyQualifiedName} should have no namespace.");
        }
    }
}