namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    public static class MySqlCorrelationPropertyTypeConverter
    {
        public static string GetColumnType(CorrelationPropertyType propertyType) => propertyType switch
        {
            CorrelationPropertyType.DateTime => "datetime",
            CorrelationPropertyType.String => "varchar(200) character set utf8mb4",
            CorrelationPropertyType.Int => "bigint(20)",
            CorrelationPropertyType.Guid => "varchar(38) character set ascii",
            CorrelationPropertyType.DateTimeOffset => throw new Exception($"Could not convert {propertyType}."),
            _ => throw new Exception($"Could not convert {propertyType}.")
        };
    }
}