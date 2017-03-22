using System.IO;
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
        tableName = saga.Name.ToUpper();
        // TODO: Can't deal with length issue this way
        if (tableName.Length > 27)
        {
            tableName = tableName.Substring(0, 27);
        }
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
select count(*) into n from ALL_TAB_COLUMNS where TABLE_NAME = '{tableName}' and COLUMN_NAME = '{name}';
if(n = 0)
then
  sqlStatement := 'alter table {tableName} add ( {name} {columnType} )';
  
  execute immediate sqlStatement;
end if;
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = OracleCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var name = OracleCorrelationPropertyName(correlationProperty);

        writer.Write($@"
select DATA_TYPE ||
  case when CHAR_LENGTH > 0 then 
    '(' || CHAR_LENGTH || ')' 
  else
    case when DATA_PRECISION is not null then
      '(' || DATA_PRECISION ||
        case when DATA_SCALE is not null and DATA_SCALE > 0 then
          ',' || DATA_SCALE
        end || ')'
    end
  end into dataType
from ALL_TAB_COLUMNS 
where TABLE_NAME = '{tableName}' and COLUMN_NAME = '{name}';

if(dataType <> '{columnType}')
then
  raise_application_error(-20000, 'Incorrect Correlation Property data type');
end if;
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        var name = OracleCorrelationPropertyName(correlationProperty);
        var indexType = "CP";
        if (correlationProperty == saga.TransitionalCorrelationProperty)
        {
            indexType = "TP";
        }

        writer.Write($@"
select count(*) into n from user_indexes where index_name = '{tableName}_{indexType}';
if(n = 0)
then
  sqlStatement := 'create unique index {tableName}_{indexType} on {tableName} ({name} ASC)';

  execute immediate sqlStatement;
end if;
");
    }

    public void WritePurgeObsoleteProperties()
    {
        var builder = new StringBuilder();

        if (saga.CorrelationProperty != null)
        {
            builder.Append($" and\r\n        column_name <> 'CORR_{saga.CorrelationProperty.Name.ToUpper()}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" and\r\n        column_name <> N'CORR_{saga.TransitionalCorrelationProperty.Name.ToUpper()}'");
        }
        writer.Write($@"
select count(*) into n
from ALL_TAB_COLUMNS
where TABLE_NAME = '{tableName}' and COLUMN_NAME LIKE 'CORR_%'{builder};
  
if(n > 0)
then

  select 'alter table {tableName} drop column ' || COLUMN_NAME into sqlStatement
  from ALL_TAB_COLUMNS
  where TABLE_NAME = '{tableName}' and COLUMN_NAME LIKE 'CORR_%'{builder};
    
  execute immediate sqlStatement;

end if;
");
    }

    public void WritePurgeObsoleteIndex()
    {
        if (saga.TransitionalCorrelationProperty != null)
        {
            return;
        }

        writer.Write($@"
select count(*) into n from user_indexes where index_name = '{tableName}_TP';
if(n > 0)
then
  sqlStatement := 'drop index {tableName}_TP';

  execute immediate sqlStatement;
end if;
");
    }

    public void WriteCreateTable()
    {
        writer.Write($@"
  select count(*) into n from user_tables where table_name = '{tableName}';
  if(n = 0)
  then
    
    sqlStatement :=
       'CREATE TABLE {tableName} 
		(
          ID VARCHAR2(38) NOT NULL 
        , METADATA CLOB NOT NULL 
        , DATA CLOB NOT NULL 
        , PERSISTENCEVERSION VARCHAR2(23) NOT NULL 
        , SAGATYPEVERSION VARCHAR2(23) NOT NULL 
        , CONCURRENCY NUMBER(9) NOT NULL 
        , CONSTRAINT {tableName}_PK PRIMARY KEY 
          (
            ID 
          )
          ENABLE 
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
    execute immediate 'drop table {tableName}';
  end if;
end;
");
    }

    private string OracleCorrelationPropertyName(CorrelationProperty property)
    {
        var name = "CORR_" + property.Name.ToUpper();
        if (name.Length > 30)
        {
            name = name.Substring(0, 30);
        }
        return name;
    }
}