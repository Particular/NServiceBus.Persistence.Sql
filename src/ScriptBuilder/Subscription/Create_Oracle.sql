declare 
  tableName varchar2(30) := :1 || 'SS';
  createTable varchar2(500);
  n number(10);
begin
  select count(*) into n from user_tables where table_name = tableName;
  if(n = 0)
  then
    
    createTable :=
       'CREATE TABLE ' || tableName || ' 
        (
          MESSAGETYPE NVARCHAR2(450) NOT NULL 
        , SUBSCRIBER NVARCHAR2(450) NOT NULL 
        , ENDPOINT VARCHAR2(450) NOT NULL 
        , PERSISTENCEVERSION VARCHAR2(23) 
        , CONSTRAINT ' || tableName || '_PK PRIMARY KEY 
          (
            MESSAGETYPE 
          , SUBSCRIBER 
          )
          ENABLE 
        ) 
        ORGANIZATION INDEX';
    
    execute immediate createTable;
    
  end if;
end;