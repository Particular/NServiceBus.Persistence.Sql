namespace NServiceBus.Persistence.Sql.CommandLine
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using ScriptBuilder;

    public enum DialectTypes
    {
        SqlServer,
        MySql,
        Oracle,
        PostgreSql,
    }

    public static class DialectTypesExtensions
    {
        static readonly Dictionary<DialectTypes, BuildSqlDialect> DialectMap = new Dictionary<DialectTypes, BuildSqlDialect>
        {
            [DialectTypes.Oracle] = BuildSqlDialect.Oracle,
            [DialectTypes.MySql] = BuildSqlDialect.MySql,
            [DialectTypes.MySql] = BuildSqlDialect.MySql,
            [DialectTypes.PostgreSql] = BuildSqlDialect.PostgreSql,
            [DialectTypes.SqlServer] = BuildSqlDialect.MsSqlServer,
        };

        public static BuildSqlDialect ToBuildSqlDialect(this DialectTypes dialect) => DialectMap[dialect];

        public static IReadOnlyList<BuildSqlDialect> ToBuildSqlDialects(this IReadOnlyList<DialectTypes> dialects)
        {
            return dialects?.Select(d => d.ToBuildSqlDialect()).ToList().AsReadOnly();
        }
    }
}