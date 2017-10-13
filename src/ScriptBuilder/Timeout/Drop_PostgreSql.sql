create or replace function drop_timeouts_table(tablePrefix varchar)
  returns integer as
  $body$
    declare
      tableNameNonQuoted varchar;
      dropTable text;
    begin
        tableNameNonQuoted := tablePrefix || 'TimeoutData';
        dropTable = 'drop table if exists  public."' || tableNameNonQuoted || '";';
        execute dropTable;
        return 0;
    end;
  $body$
  language 'plpgsql';

select drop_timeouts_table(@tablePrefix);