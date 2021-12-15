using System;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQLServerConnectionString")))
        {
            using (var connection = MsSqlSystemDataClientConnectionBuilder.Build())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
if not exists (
    select  *
    from sys.schemas
    where name = 'schema_name')
exec('create schema schema_name');";
                    command.ExecuteNonQuery();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PostgreSqlConnectionString")))
        {
            using (var connection = PostgreSqlConnectionBuilder.Build())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"create schema if not exists ""SchemaName"";";
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}