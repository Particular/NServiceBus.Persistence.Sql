using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        using (var connection = MsSqlConnectionBuilder.Build())
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

}