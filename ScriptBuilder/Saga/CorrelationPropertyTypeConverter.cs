using System;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class CorrelationPropertyTypeConverter
{

    public static string GetColumnType(CorrelationPropertyType propertyType)
    {
        switch (propertyType)
        {
            case CorrelationPropertyType.DateTime:
                return "datetime";
            case CorrelationPropertyType.DateTimeOffset:
                return "datetimeoffset";
            case CorrelationPropertyType.String:
                return "nvarchar(450)";
            case CorrelationPropertyType.Int:
                return "bigint";
            case CorrelationPropertyType.Guid:
                return "uniqueidentifier";
        }
        throw new Exception($"Could not convert {propertyType}.");
    }
}