using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const string UNDERLINE = "\u001b[4m";
const string RED = "\u001b[31m";
const string YELLOW = "\u001b[33m";
const string RESET = "\u001b[0m";

Console.WriteLine(UNDERLINE + "\nPlease contact Particular support if assistance is required.\n" + RESET);

if (args.Length < 2)
{
    Console.WriteLine(@"sagajsonstatepatch.exe ""connection string"" ""table name""

Example:

    sagajsonstatepatch.exe ""%SQLServerConnectionString%"" NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga
");
    return -2;
}

bool whatIf = true;
var connectionString = args[0];
var tableName = args[1];

Console.WriteLine("Connection string: " + connectionString);
Console.WriteLine("Table name       : " + tableName);

Console.Write("Dry run? [Y/n]");
var result = Console.ReadLine();
whatIf = result != "n";
Console.WriteLine("Dry run: " + whatIf);

var start = Stopwatch.StartNew();
using var con = new SqlConnection(connectionString);

await con.OpenAsync();

var cmd = con.CreateCommand();
cmd.CommandText = $"SELECT Id, Data FROM {tableName} WHERE PersistenceVersion = '7.0.3.0' and LEN(Data)=4000 ORDER BY Id";

int processed = 0;
int success = 0;
int failed = 0;

using var reader = cmd.ExecuteReader();

while (reader.Read())
{
    ++processed;
    var id = (Guid)reader[0];
    var json = (string)reader[1];

    json = json.Trim('\0');

    var originalLength = json.Length;

    int i;

    try
    {
        JObject.Parse(json);
        // Successful parse means the JSON data is correct, but we still need to truncate
        i = json.Length;
    }
    catch (JsonReaderException)
    {
        // If this happens, lets continue to patch
        var open = json.Where(c => c == '{').Count();
        var close = json.Where(c => c == '}').Count();

        if (close <= open)
        {
            ++failed;
            Console.WriteLine(RED + "Closing / open bracket count condition: Cannot patch json for {0}, manual patching required." + RESET, id);
            continue;
        }

        var diff = close - open;

        i = json.Length;

        for (var iteration = 0; iteration <= diff; iteration++)
        {
            i = json.LastIndexOf('}', --i);
        }

        json = json.Substring(0, ++i);

        try
        {
            JObject.Parse(json);
        }
        catch (JsonReaderException)
        {
            ++failed;
            Console.WriteLine(RED + "Invalid json after truncate: cannot patch json for {0}, manual patching required." + RESET, id);
            continue;
        }
    }

    if (!whatIf)
    {
        using var con2 = new SqlConnection(connectionString);
        using var update = con2.CreateCommand();
        update.CommandText = $"UPDATE {tableName} SET Data = left(Data, @len) WHERE Id=@id";
        update.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;
        update.Parameters.Add("@len", SqlDbType.Int).Value = i;
        await con2.OpenAsync();
        await update.ExecuteNonQueryAsync();
        if (originalLength != i) Console.Write(YELLOW);
        Console.WriteLine($"Truncated json `{id}` from {originalLength} to {i:N0} characters, original data size was 4.000 nvarchar (8.000 bytes)" + RESET);
    }
    else
    {
        Console.WriteLine($"UPDATE {tableName} SET Data = left(Data, {i}) WHERE Id={id}");
    }

    ++success;
}

Console.WriteLine($"\nProcess {processed} matching rows in {start.Elapsed}, patched {success} records and {failed} need manual patching.");

return failed == 0 ? 0 : -1;