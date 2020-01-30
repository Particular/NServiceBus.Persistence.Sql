using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
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