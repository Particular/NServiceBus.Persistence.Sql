using NUnit.Framework;
using System.Data.SqlClient;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        MsSqlConnectionBuilder.CreateDbIfNotExists();

        using (var conn = MsSqlConnectionBuilder.Build())
        {
            conn.Open();
            conn.ExecuteCommand($"ALTER DATABASE [{conn.Database}] COLLATE SQL_Latin1_General_CP1_CS_AS;");
        }
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        using (var conn = MsSqlConnectionBuilder.Build())
        {
            conn.Open();
            conn.ExecuteCommand($"use master; if exists (select * from sysdatabases where name = '{conn.Database}') begin alter database {conn.Database} set SINGLE_USER with rollback immediate; drop database {conn.Database}; end;");
        }
    }
}