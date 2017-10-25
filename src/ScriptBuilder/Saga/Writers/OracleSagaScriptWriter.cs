using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class OracleSagaScriptWriter : ISagaScriptWriter
{
    TextWriter writer;
    SagaDefinition saga;
    string tableName;

    public OracleSagaScriptWriter(TextWriter textWriter, SagaDefinition saga)
    {
        writer = textWriter;
        this.saga = saga;
        if (saga.TableSuffix.Length > 27)
        {
            throw new Exception($"Saga '{saga.TableSuffix}' contains more than 27 characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, shorten the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
        }
        if (Encoding.UTF8.GetBytes(saga.TableSuffix).Length != saga.TableSuffix.Length)
        {
            throw new Exception($"Saga '{saga.TableSuffix}' contains non-ASCII characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, change the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
        }
        tableName = saga.TableSuffix.ToUpper();
    }

    public void Initialize()
    {
        writer.WriteLine(@"
declare
  sqlStatement varchar2(500);
  dataType varchar2(30);
  n number(10);
begin");
    }

    public void WriteTableNameVariable()
    {
    }

    public void CreateComplete()
    {
        writer.WriteLine("end;");
    }

    public void AddProperty(CorrelationProperty correlationProperty)
    {
        var columnType = OracleCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var name = OracleCorrelationPropertyName(correlationProperty);
        writer.Write($@"
select count(*) into n from all_tab_columns where table_name = '{tableName}' and column_name = '{name}';
if(n = 0)
then
  sqlStatement := 'alter table ""{tableName}"" add ( {name} {columnType} )';

  execute immediate sqlStatement;
end if;
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = OracleCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var name = OracleCorrelationPropertyName(correlationProperty);

        writer.Write($@"
select data_type ||
  case when char_length > 0 then
    '(' || char_length || ')'
  else
    case when data_precision is not null then
      '(' || data_precision ||
        case when data_scale is not null and data_scale > 0 then
          ',' || data_scale
        end || ')'
    end
  end into dataType
from all_tab_columns
where table_name = '{tableName}' and column_name = '{name}';

if(dataType <> '{columnType}')
then
  raise_application_error(-20000, 'Incorrect Correlation Property data type');
end if;
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        var columnName = OracleCorrelationPropertyName(correlationProperty);
        var indexName = CreateSagaIndexName(tableName, correlationProperty.Name);

        writer.Write($@"
select count(*) into n from user_indexes where table_name = '{tableName}' and index_name = '{indexName}';
if(n = 0)
then
  sqlStatement := 'create unique index ""{indexName}"" on ""{tableName}"" ({columnName} ASC)';

  execute immediate sqlStatement;
end if;
");
    }

    public void WritePurgeObsoleteProperties()
    {
        var builder = new StringBuilder();

        if (saga.CorrelationProperty != null)
        {
            builder.Append($" and\r\n        column_name <> '{OracleCorrelationPropertyName(saga.CorrelationProperty)}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" and\r\n        column_name <> '{OracleCorrelationPropertyName(saga.TransitionalCorrelationProperty)}'");
        }
        writer.Write($@"
select count(*) into n
from all_tab_columns
where table_name = '{tableName}' and column_name like 'CORR_%'{builder};

if(n > 0)
then

  select 'alter table ""{tableName}"" drop column ' || column_name into sqlStatement
  from all_tab_columns
  where table_name = '{tableName}' and column_name like 'CORR_%'{builder};

  execute immediate sqlStatement;

end if;
");
    }

    public void WritePurgeObsoleteIndex()
    {
        // Dropping the column with the index attached removes the index as well
    }

    public void WriteCreateTable()
    {
        writer.Write($@"
  select count(*) into n from user_tables where table_name = '{tableName}';
  if(n = 0)
  then

    sqlStatement :=
       'create table ""{tableName}""
       (
          id varchar2(38) not null,
          metadata clob not null,
          data clob not null,
          persistenceversion varchar2(23) not null,
          sagatypeversion varchar2(23) not null,
          concurrency number(9) not null,
          constraint ""{tableName}_PK"" primary key
          (
            id
          )
          enable
        )';

    execute immediate sqlStatement;

  end if;
");
    }

    public void WriteDropTable()
    {
        writer.Write($@"
declare
  n number(10);
begin
  select count(*) into n from user_tables where table_name = '{tableName}';
  if(n > 0)
  then
    execute immediate 'drop table ""{tableName}""';
  end if;
end;
");
    }

    string OracleCorrelationPropertyName(CorrelationProperty property)
    {
        var name = "CORR_" + property.Name.ToUpper();
        if (name.Length > 30)
        {
            name = name.Substring(0, 30);
        }
        return name;
    }

    // Generates a 30ch index name like "SAGAIDX_66462BF46C9DCB70FD257D" based on
    // SHA1 hash of saga name and correlation property name
    string CreateSagaIndexName(string sagaName, string correlationPropertyName)
    {
        var sb = new StringBuilder("SAGAIDX_", 30);
        var clearText = Encoding.UTF8.GetBytes($"{sagaName}/{correlationPropertyName}");
        using (var sha1 = SHA1.Create())
        {
            var hashBytes = sha1.ComputeHash(clearText);
            // SHA1 hash contains 20 bytes, but only have space in 30 char index name for 11 bytes => 22 chars
            for (var i = 0; i < 11; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
        }
        return sb.ToString();
    }
}