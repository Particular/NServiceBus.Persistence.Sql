namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    public static class OracleCorrelationPropertyTypeConverter
    {
        public static string GetColumnType(CorrelationPropertyType propertyType) => propertyType switch
        {
            CorrelationPropertyType.DateTime => "TIMESTAMP",
            CorrelationPropertyType.String => "NVARCHAR2(200)",
            CorrelationPropertyType.Int => "NUMBER(19)",
            CorrelationPropertyType.Guid => "VARCHAR2(38)",
            CorrelationPropertyType.DateTimeOffset => throw new Exception($"Could not convert {propertyType}."),
            _ => throw new Exception($"Could not convert {propertyType}.")
        };
    }
}