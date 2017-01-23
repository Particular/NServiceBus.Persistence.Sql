using System;
using MySql.Data.MySqlClient;

static class MySqlConnectionBuilder
{
    public static MySqlConnection Build()
    {
        var password = Environment.GetEnvironmentVariable("MySqlPassword");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new Exception("Could not extra 'MySqlPassword' from Environment variables.");
        }
        var username = Environment.GetEnvironmentVariable("MySqlUserName");
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new Exception("Could not extra 'MySqlUserName' from Environment variables.");
        }
        var connection = $"server=localhost;user={username};database=sqlpersistencetests;port=3306;password={password};AllowUserVariables=True;AutoEnlist=false";
        return new MySqlConnection(connection);
    }
}