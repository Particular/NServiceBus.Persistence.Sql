using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

public static class SqlValidator
{
    public static void Validate(string sql)
    {
        var parser = new TSql140Parser(false);
        IList<ParseError> errors;
        using (var reader = new StringReader(sql))
        {
            parser.Parse(reader, out errors);
        }
        if (errors == null || errors.Count == 0)
        {
            return;
        }
        var message = $"Sql errors:{string.Join("\r\n", errors.Select(error => error.Message))}";
        throw new Exception(message);
    }
}