using System;

static class Guard
{
    public static void AgainstSqlDelimiters(string argumentName, string value)
    {
        if (value.Contains("]") || value.Contains("[") || value.Contains("`"))
        {
            throw new ArgumentException($"The argument '{argumentName}' contains a ']', '[' or '`'. Names and schemas automatically quoted.");
        }
    }
}