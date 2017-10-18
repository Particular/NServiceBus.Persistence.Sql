create or replace function create_subscription_table(tablePrefix varchar)
  returns integer as
  $body$
    declare
      tableNameNonQuoted varchar;
      createTable text;
    begin
        tableNameNonQuoted := tablePrefix || 'SubscriptionData';
        createTable = 'create table if not exists public."' || tableNameNonQuoted || '"
    (
        "Id" character varying(400) not null,
        "Subscriber" character varying(200) not null,
        "Endpoint" character varying(200),
        "MessageType" character varying(200) not null,
        "PersistenceVersion" character varying(200) not null,
        primary key ("Id")
    );
';
        execute createTable;
        return 0;
    end;
  $body$
  language 'plpgsql';

select create_subscription_table(@tablePrefix);