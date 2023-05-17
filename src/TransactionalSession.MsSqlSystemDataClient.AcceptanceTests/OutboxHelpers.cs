namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using Persistence.Sql.ScriptBuilder;

    static class OutboxHelpers
    {
        public static async Task CreateOutboxTable<TEndpoint>()
            => await CreateOutboxTable(Conventions.EndpointNamingConvention(typeof(TEndpoint)));

        public static async Task CreateOutboxTable(string endpointName)
        {
            string tablePrefix = endpointName.Replace('.', '_');
            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();
            await connection.OpenAsync().ConfigureAwait(false);

            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(BuildSqlDialect.MsSqlServer), tablePrefix);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(BuildSqlDialect.MsSqlServer), tablePrefix);
        }

        public static async Task CreateDataTable()
        {
            var createTable = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SomeTable' and xtype='U')
            BEGIN
                CREATE TABLE [dbo].[SomeTable]([Id] [nvarchar](50) NOT NULL)
            END;";
            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();
            await connection.OpenAsync();
            using var createTableCommand = new SqlCommand(createTable, connection);
            await createTableCommand.ExecuteNonQueryAsync();
        }
    }
}