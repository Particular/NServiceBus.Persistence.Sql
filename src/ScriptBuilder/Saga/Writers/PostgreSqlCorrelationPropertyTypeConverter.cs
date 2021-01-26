namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    public static class PostgreSqlCorrelationPropertyTypeConverter
    {
        public static string GetColumnType(CorrelationPropertyType propertyType)
        {
            switch (propertyType)
            {
                case CorrelationPropertyType.DateTime:
                    return "timestamp";
                case CorrelationPropertyType.String:
                    return "character varying(200)";
                case CorrelationPropertyType.Int:
                    return "integer";
                case CorrelationPropertyType.Guid:
                    return "uuid";
                case CorrelationPropertyType.DateTimeOffset:
                default:
                    throw new Exception($"Could not convert {propertyType}.");
            }
        }
    }
}