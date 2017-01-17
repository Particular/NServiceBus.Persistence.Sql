using System;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    /// <summary>
    /// Not for public use.
    /// </summary>
    public static class MySqlCorrelationPropertyTypeConverter
    {
        public static string GetColumnType(CorrelationPropertyType propertyType)
        {
            switch (propertyType)
            {
                case CorrelationPropertyType.DateTime:
                    return "datetime";
                case CorrelationPropertyType.DateTimeOffset:
                    throw new Exception("DateTimeOffset is not supported by MySql.");
                case CorrelationPropertyType.String:
                    return "varchar(450)";
                case CorrelationPropertyType.Int:
                    return "bigint(20)";
                case CorrelationPropertyType.Guid:
                    return "varchar(38)";
            }
            throw new Exception($"Could not convert {propertyType}.");
        }
    }
}