using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Settings;
using NServiceBus.Transport;

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

    public static Task<DbConnection> OpenNonContextualConnection(this IConnectionManager connectionManager, CancellationToken cancellationToken = default)
    {
        var connection = connectionManager.BuildNonContextual();
        return OpenConnection(connection, cancellationToken);
    }

    public static Task<DbConnection> OpenConnection(this IConnectionManager connectionManager, IncomingMessage incomingMessage, CancellationToken cancellationToken = default)
    {
        var connection = connectionManager.Build(incomingMessage);
        return OpenConnection(connection, cancellationToken);
    }

    static async Task<DbConnection> OpenConnection(DbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
        {
            // copy the general catch but don't let another exception mask the OCE
            try
            {
                connection?.Dispose();
            }
            catch { }

            throw;
        }
        catch (Exception)
        {
            connection?.Dispose();
            throw;
        }
    }

    public static void AddParameter(this DbCommand command, string name, Version value)
    {
        command.AddParameter(name, value.ToString());
    }

    public static async Task<bool> GetBoolAsync(this DbDataReader reader, int position, CancellationToken cancellationToken = default)
    {
        var type = reader.GetFieldType(position);
        // MySql stores bools as ints
        if (type == typeof(ulong))
        {
            return Convert.ToBoolean(await reader.GetFieldValueAsync<ulong>(position, cancellationToken).ConfigureAwait(false));
        }
        // In Oracle we store bools as NUMBER(1,0) (short).
        if (type == typeof(short))
        {
            return Convert.ToBoolean(await reader.GetFieldValueAsync<short>(position, cancellationToken).ConfigureAwait(false));
        }
        return await reader.GetFieldValueAsync<bool>(position, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Guid> GetGuidAsync(this DbDataReader reader, int position, CancellationToken cancellationToken = default)
    {
        var type = reader.GetFieldType(position);
        // MySql stores Guids as strings
        if (type == typeof(string))
        {
            return new Guid(await reader.GetFieldValueAsync<string>(position, cancellationToken).ConfigureAwait(false));
        }
        return await reader.GetFieldValueAsync<Guid>(position, cancellationToken).ConfigureAwait(false);
    }

    internal static Func<T, object> GetPropertyAccessor<T>(this Type sagaDataType, string propertyName)
    {
        var propertyInfo = sagaDataType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                           ?? throw new Exception($"Expected '{sagaDataType.FullName}' to contain a gettable property named '{propertyName}'.");
        return data => propertyInfo.GetValue(data);
    }

    public static async Task<int> ExecuteNonQueryEx(this DbCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            var message = $"Failed to ExecuteNonQuery. CommandText:{Environment.NewLine}{command.CommandText}";
            throw new Exception(message, ex);
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

    public static IncomingMessage GetIncomingMessage(this ContextBag context)
    {
        if (context == null)
        {
            // Tests pass a null context
            return null;
        }

        if (context.TryGet(out IncomingMessage message))
        {
            return message;
        }

        throw new Exception("Can't find message headers from context.");
    }

    public static bool EndpointIsMultiTenant(this IReadOnlySettings settings)
    {
        return settings.GetOrDefault<bool>("SqlPersistence.MultiTenant");
    }
}
