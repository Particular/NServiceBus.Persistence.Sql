﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;

static class Extensions
{
    public static void AddParameter(this DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public static void AddParameter(this DbCommand command, string name, DateTime value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        parameter.DbType = DbType.DateTime;
        command.Parameters.Add(parameter);
    }

    public static void AddParameter(this DbCommand command, string name, Version value)
    {
        command.AddParameter(name, value.ToString());
    }

    public static async Task<bool> GetBoolAsync(this DbDataReader reader, int position)
    {
        var type = reader.GetFieldType(position);
        // MySql stores bools as ints
        if (type == typeof(ulong))
        {
            return Convert.ToBoolean(await reader.GetFieldValueAsync<ulong>(position).ConfigureAwait(false));
        }
        // In Oracle we store bools as NUMBER(1,0) (short).
        if (type == typeof(short))
        {
            return Convert.ToBoolean(await reader.GetFieldValueAsync<short>(position).ConfigureAwait(false));
        }
        return await reader.GetFieldValueAsync<bool>(position).ConfigureAwait(false);
    }

    public static async Task<Guid> GetGuidAsync(this DbDataReader reader, int position)
    {
        var type = reader.GetFieldType(position);
        // MySql stores Guids as strings
        if (type == typeof(string))
        {
            return new Guid(await reader.GetFieldValueAsync<string>(position).ConfigureAwait(false));
        }
        return await reader.GetFieldValueAsync<Guid>(position).ConfigureAwait(false);
    }

    internal static Func<T, object> GetPropertyAccessor<T>(this Type sagaDataType, string propertyName)
    {
        var propertyInfo = sagaDataType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (propertyInfo == null)
        {
            throw new Exception($"Expected '{sagaDataType.FullName}' to contain a gettable property named '{propertyName}'.");
        }
        return data => propertyInfo.GetValue(data);
    }

    public static Task ExecuteNonQueryEx(this DbCommand command)
    {
        return ExecuteNonQueryEx(command, CancellationToken.None);
    }

    public static async Task<int> ExecuteNonQueryEx(this DbCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var message = $"Failed to ExecuteNonQuery. CommandText:{Environment.NewLine}{command.CommandText}";
            throw new Exception(message, exception);
        }
    }

    public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            Type current;
            if (toCheck.IsGenericType)
            {
                current = toCheck.GetGenericTypeDefinition();
            }
            else
            {
                current = toCheck;
            }
            if (generic == current)
            {
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        return false;
    }

    public static IReadOnlyDictionary<string, string> GetMessageHeaders(this ContextBag context)
    {
        if (context is ITransportReceiveContext transportReceiveContext)
        {
            return transportReceiveContext.Message.Headers;
        }

        if (context is IIncomingContext incomingContext)
        {
            return incomingContext.MessageHeaders;
        }

        if (context == null)
        {
            // Tests pass a null context
            return null;
        }

        //if (context.GetType() == typeof(ContextBag))
        //{
        //    return null;
        //}

        throw new Exception("Can't find incoming context");
    }
}
