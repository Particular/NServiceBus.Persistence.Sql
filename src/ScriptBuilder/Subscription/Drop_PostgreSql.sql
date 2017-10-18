﻿create or replace function pg_temp.drop_subscription_table(tablePrefix varchar)
  returns integer as
  $body$
    declare
      tableNameNonQuoted varchar;
      dropTable text;
    begin
        tableNameNonQuoted := tablePrefix || 'SubscriptionData';
        dropTable = 'drop table if exists public."' || tableNameNonQuoted || '";';
        execute dropTable;
        return 0;
    end;
  $body$
  language 'plpgsql';

select pg_temp.drop_subscription_table(@tablePrefix);