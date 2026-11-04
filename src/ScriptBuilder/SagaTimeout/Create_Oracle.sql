declare
  tableName varchar2(30) := UPPER(:tablePrefix) || 'ST';
  n number(10);
begin
  select count(*) into n from user_tables where table_name = tableName;
  if(n = 0)
  then

    execute immediate
       'create table "' || tableName || '"
       (
         sagaid varchar2(38) not null,
         timeoutid nvarchar2(200) not null,
         data clob not null,
         persistenceversion varchar2(23) not null,
         constraint "' || tableName || '_PK" primary key
         (
           sagaid,
           timeoutid
         )
         enable
       )';

  end if;
end;
