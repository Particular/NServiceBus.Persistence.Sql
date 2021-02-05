declare
  tableName varchar2(128) := UPPER(:tablePrefix) || 'TO';
  dropTable varchar2(150);
  n number(10);
begin
  select count(*) into n from user_tables where table_name = tableName;
  if(n = 1)
  then

    dropTable := 'drop table "' || tableName || '"';

    execute immediate dropTable;

  end if;
end;