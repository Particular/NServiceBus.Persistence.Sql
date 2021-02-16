using System.IO;

public static class PathExtensions
{
    public static string ConvertPathSeparators(this string path, string toChar)
    {
        return path?.Replace("\\", toChar);
    }
}