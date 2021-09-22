namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    public static class PostgreSqlCorrelationPropertyTypeConverter
    {
        public static string GetColumnType(CorrelationPropertyType propertyType) => propertyType switch
        {
            CorrelationPropertyType.DateTime => "timestamp",
            CorrelationPropertyType.String => "character varying(200)",
            CorrelationPropertyType.Int => "integer",
            CorrelationPropertyType.Guid => "uuid",
            CorrelationPropertyType.DateTimeOffset => throw new Exception($"Could not convert {propertyType}."),
            _ => throw new Exception($"Could not convert {propertyType}.")
        };
    }
}