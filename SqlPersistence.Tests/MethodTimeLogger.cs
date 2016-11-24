using System.Diagnostics;
using System.Reflection;

public static class MethodTimeLogger
{
    public static void Log(MethodBase methodBase, long milliseconds)
    {
        Trace.WriteLine($"{methodBase.DeclaringType.Name}.{methodBase.Name} {milliseconds}ms");
    }
}
