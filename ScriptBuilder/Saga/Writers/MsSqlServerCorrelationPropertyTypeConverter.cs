using System;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    /// <summary>
    /// Not for public use.
    /// </summary>
    public class MsSqlServerCorrelationPropertyTypeConverter
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
}