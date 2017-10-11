create or replace function create_subscription_table(tablePrefix varchar)
  returns integer as
  $body$
    declare
      tableNameNonQuoted varchar;
      createTable text;
    begin
        tableNameNonQuoted := tablePrefix || 'SubscriptionData';
        createTable = 'CREATE TABLE IF NOT EXISTS public.' || tableNameNonQuoted || '
    (
        "Id" character varying(400) NOT NULL,
        "Subscriber" character varying(200) NOT NULL,
        "Endpoint" character varying(200),
        "MessageType" character varying(200) NOT NULL,
        "PersistenceVersion" character varying(200) NOT NULL,
        PRIMARY KEY ("Id")
    )
    WITH (
        OIDS = FALSE
    );
';
        execute createTable;
        return 0;
    end;
  $body$
  language 'plpgsql';

select create_subscription_table(@tablePrefix);