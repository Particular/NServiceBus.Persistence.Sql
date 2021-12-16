namespace PostgreSql
{
    using NUnit.Framework;

    [SetUpFixture, PostgreSqlTest]
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
    using NUnit.Framework;

    [SetUpFixture, SqlServerTest]
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
    using NUnit.Framework;

    [SetUpFixture, SqlServerTest]
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
    using NUnit.Framework;

    [SetUpFixture, MySqlTest]
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
    using NUnit.Framework;

    [SetUpFixture, OracleTest]
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