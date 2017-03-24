declare 
  tableName varchar2(30) := :1 || 'OD';
  indexName varchar2(30) := tableName || '_IDX';
  dropTable varchar2(50);
  dropIndex varchar2(50);
  n number(10);
begin
  select count(*) into n from user_indexes where index_name = UPPER(indexName);
  if(n = 1)
  then

    dropIndex :=
      'DROP INDEX ' || indexName;

    execute immediate dropIndex;

  end if;

  select count(*) into n from user_tables where table_name = UPPER(tableName);
  if(n = 1)
  then
    
    dropTable := 'DROP TABLE ' || tableName;
    
    execute immediate dropTable;
    
  end if;
end;