namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using Persistence.Sql.ScriptBuilder;

    static class OutboxHelpers
    {
        public static async Task CreateOutboxTable<TEndpoint>()
            => await CreateOutboxTable(Conventions.EndpointNamingConvention(typeof(TEndpoint)));

        public static async Task CreateOutboxTable(string endpointName)
        {
            string tablePrefix = TestTableNameCleaner.Clean(endpointName);
            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();
            await connection.OpenAsync().ConfigureAwait(false);

            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(BuildSqlDialect.MsSqlServer), tablePrefix);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(BuildSqlDialect.MsSqlServer), tablePrefix);
        }
    }
}