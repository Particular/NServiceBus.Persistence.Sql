create or replace function create_outbox_table(tablePrefix varchar)
  returns integer as
  $body$
    declare
      tableNameNonQuoted varchar;
      createTable text;
    begin
        tableNameNonQuoted := tablePrefix || 'OutboxData';
        createTable = 'CREATE TABLE IF NOT EXISTS public.' || tableNameNonQuoted || '
    (
        "MessageId" character varying(200),
        "Dispatched" boolean not null default false,
        "DispatchedAt" timestamp,
        "PersistenceVersion" character varying(23),
        "Operations" jsonb not null,
        PRIMARY KEY ("MessageId")
    )
    WITH (
        OIDS = FALSE
    );
    CREATE INDEX IF NOT EXISTS "Index_DispatchedAt" ON public.' || tableNameNonQuoted || ' USING btree ("DispatchedAt" ASC NULLS LAST);
    CREATE INDEX IF NOT EXISTS "Index_Dispatched" ON public.' || tableNameNonQuoted || ' USING btree ("Dispatched" ASC NULLS LAST);
';
		execute createTable;
        return 0;
    end;
  $body$
  language 'plpgsql';

select create_outbox_table(@tablePrefix);