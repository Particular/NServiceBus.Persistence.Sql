using NUnit.Framework;

namespace PostgreSql
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Setup()
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

namespace SqlServerSystemData
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Setup()
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
    }
}

namespace SqlServerMicrosoftData
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Setup()
        {
            using (var connection = MsSqlMicrosoftDataClientConnectionBuilder.Build())
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
}

namespace MySql
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Setup()
        {
            using (var connection = MySqlConnectionBuilder.Build())
            {
                connection.Open();
            }
        }
    }
}

namespace Oracle
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Setup()
        {
            using (var connection = OracleConnectionBuilder.Build())
            {
                connection.Open();
            }
        }
    }
}