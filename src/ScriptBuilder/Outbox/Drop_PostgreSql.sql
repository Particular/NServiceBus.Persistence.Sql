create or replace function drop_outbox_table(tablePrefix varchar)
  returns integer as
  $body$
    declare
      tableNameNonQuoted varchar;
      dropTable text;
    begin
        tableNameNonQuoted := tablePrefix || 'OutboxData';
        dropTable = 'DROP TABLE IF EXISTS public.' || tableNameNonQuoted || ';';
		execute dropTable;
        return 0;
    end;
  $body$
  language 'plpgsql';

select drop_outbox_table(@tablePrefix);
