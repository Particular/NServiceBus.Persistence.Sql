using System.Diagnostics;
using System.Reflection;

public static class MethodTimeLogger
{
    public static void Log(MethodBase methodBase, long milliseconds)
    {
        Trace.WriteLine(string.Format("{0}.{1} {2}ms", methodBase.DeclaringType.Name, methodBase.Name, milliseconds));
    }
}
