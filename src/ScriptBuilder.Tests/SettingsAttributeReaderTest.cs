using System;
using NUnit.Framework;

[assembly: GenericMarker<int>]

[TestFixture]
public class SettingsAttributeReaderTest
{
    [Test]
    public void Ignores_assembly_level_generic_attributes()
    {
        var assemblyPath = typeof(SettingsAttributeReaderTest).Assembly.Location;

        var result = SettingsAttributeReader.Read(assemblyPath);

        Assert.That(result, Is.Not.Null);
    }
}

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GenericMarkerAttribute<T> : Attribute;
