declare 
  tableName varchar2(30) := :1 || 'SS';
  dropTable varchar2(500);
  n number(10);
begin
  select count(*) into n from user_tables where table_name = tableName;
  if(n = 1)
  then
    
    dropTable := 'DROP TABLE ' || tableName;
    
    execute immediate dropTable;
    
  end if;
end;