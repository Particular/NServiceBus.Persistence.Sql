using System;
using System.Data.SqlClient;
using System.Reflection;

static class Extensions
{
    public static void AddParameter(this SqlCommand command, string name, object value)
    {
        command.Parameters.AddWithValue(name, value);
    }

    public static bool ContainsAttribute<T>(this MemberInfo propertyInfo) where T:Attribute
    {
        return Attribute.IsDefined(propertyInfo, typeof (T));
    }

    public static void AddParameter(this SqlCommand command, string name, Version value)
    {
        command.Parameters.AddWithValue(name, value.ToString());
    }

    public static void ExecuteNonQueryEx(this SqlCommand command)
    {
        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception exception)
        {
            var message = $"Failed to ExecuteNonQuery. CommandText:{Environment.NewLine}{command.CommandText}";
            throw new Exception(message, exception);
        }
    }
}