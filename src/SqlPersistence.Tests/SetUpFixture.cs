using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        FixCurrentDirectory();
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

    void FixCurrentDirectory([CallerFilePath] string callerFilePath="")
    {
        Environment.CurrentDirectory = Directory.GetParent(callerFilePath).FullName;
    }
}