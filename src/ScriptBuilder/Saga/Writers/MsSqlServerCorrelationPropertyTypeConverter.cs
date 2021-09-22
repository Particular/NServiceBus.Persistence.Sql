namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    public static class MsSqlServerCorrelationPropertyTypeConverter
    {
        public static string GetColumnType(CorrelationPropertyType propertyType) => propertyType switch
        {
            CorrelationPropertyType.DateTime => "datetime",
            CorrelationPropertyType.DateTimeOffset => "datetimeoffset",
            CorrelationPropertyType.String => "nvarchar(200)",
            CorrelationPropertyType.Int => "bigint",
            CorrelationPropertyType.Guid => "uniqueidentifier",
            _ => throw new Exception($"Could not convert {propertyType}.")
        };
    }
}