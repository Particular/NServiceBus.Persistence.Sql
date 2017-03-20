declare 
  tableName varchar2(30) := :1 || 'TO';
  createTable varchar2(500);
  n number(10);
begin
  select count(*) into n from user_tables where table_name = tableName;
  if(n = 0)
  then
    
    createTable :=
       'CREATE TABLE ' || tableName || ' 
		(
		  ID VARCHAR2(38) NOT NULL 
		, DESTINATION NVARCHAR2(200) NOT NULL 
		, SAGAID VARCHAR2(38) 
		, STATE BLOB 
		, EXPIRETIME DATE 
		, HEADERS CLOB NOT NULL 
		, PERSISTENCEVERSION VARCHAR2(23) NOT NULL 
		, CONSTRAINT TIMEOUTDATA2_PK PRIMARY KEY 
		  (
			ID 
		  )
		  ENABLE 
		)';
    
    execute immediate createTable;
    
  end if;
end;