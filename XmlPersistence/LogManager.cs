using NServiceBus.Logging;

static class LogManager
{
    public static ILog GetLogger<T>()
    {
        var type = typeof(T);
        if (type.Namespace == null)
        {
            var name = $"NServiceBus.Persistence.Sql.Xml.{type.Name}";
            return NServiceBus.Logging.LogManager.GetLogger(name);
        }
        return NServiceBus.Logging.LogManager.GetLogger(type);
    }
}