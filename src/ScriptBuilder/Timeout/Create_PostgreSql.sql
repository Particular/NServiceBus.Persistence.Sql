create or replace function create_timeouts_table(tablePrefix varchar)
  returns integer as
  $body$
    declare
      tableNameNonQuoted varchar;
      createTable text;
    begin
        tableNameNonQuoted := tablePrefix || 'TimeoutData';
        createTable = 'create table if not exists public."' || tableNameNonQuoted || '"
    (
        "Id" uuid not null,
        "Destination" character varying(200),
        "SagaId" uuid,
        "State" bytea,
        "Time" timestamp,
        "Headers" text,
        "PersistenceVersion" character varying(23),
        primary key ("Id")
    );
    create index if not exists "Time_Idx" on public."' || tableNameNonQuoted || '" using btree ("Time" asc nulls last);
    create index if not exists "SagaId_Idx" on public."' || tableNameNonQuoted || '" using btree ("SagaId" asc nulls last);
';
        execute createTable;
        return 0;
    end;
  $body$
  language 'plpgsql';

select create_timeouts_table(@tablePrefix);