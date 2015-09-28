using System;
using System.Diagnostics;
using System.Reflection;

static class StaticVersions
{
    public static Version PeristenceVersion;

    static StaticVersions()
    {
        var assembly = Assembly.GetExecutingAssembly();
        PeristenceVersion = GetFileVersion(assembly);
    }

    internal static Version GetFileVersion(this Assembly assembly)
    {
        var version = FileVersionInfo.GetVersionInfo(assembly.Location);
        return new Version(
            major: version.FileMajorPart, 
            minor: version.FileMinorPart, 
            build: version.FileBuildPart, 
            revision: version.FilePrivatePart);
    }
}