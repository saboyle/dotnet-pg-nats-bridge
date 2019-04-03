# dotnet-pg-nats-bridge [alpha]

Exploratory implementation of a .net Postgres to NATS bridge.

This is a .net version of an asyncio asyncpg python version (https://github.com/saboyle/asyncio-pg-nats-bridge).

A minor difference is the NOTIFY channel used in this example has been changed to exclude the period '.' as it seems to cause problems with the Npgsql package. 

``` sql
#### Database setup
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
-- CREATE TABLE fixtures (fixture_id uuid primary key, params json not null);
DROP TABLE fixtures;
CREATE TABLE fixtures (fixture_id int primary key, params int not null);


#### Create trigger with Notify on Create, Update or Delete
begin;

create or replace function tg_notify ()
 returns trigger
 language plpgsql
as $$
declare
  channel text := TG_ARGV[0];
begin
  PERFORM (
     with payload(key, params) as
     (
       select NEW.fixture_id, NEW.params as fixs
     )
     select pg_notify(channel, row_to_json(payload)::text)
       from payload
  );
  RETURN NULL;
end;
$$;

CREATE TRIGGER notify_fixtures
         AFTER INSERT
            ON fixtures
      FOR EACH ROW
       EXECUTE PROCEDURE tg_notify('fixtures');

commit;

-- To setup listeners to Async notify channel.
LISTEN fixtures;

-- Inserting a dummy record to test async notifications.
insert into fixtures values (4,1);
```

## Usage:
``` cmd
c:>pg_nats_bridge.exe -h

Usage:  [options]

Options:
  -?|-h|--help                            Show help information
  -pgHost|--postgresHost <host>           Postgres host
  -pgPort|--postgresPort <port>           Postgres port
  -pgChannel|--postgresChannel <channel>  Postgres notification channel
  -pgDB|--postgresDBl <channel>           Postgres database
  -pgUser|--postgresUser <user>           Postgres user
  -pgPassword|--postgresPassword <user>   Postgres password
  -msgHost|--msgHost <host>               NATS Host
  -msgPort|--msgPort <host>               NATS Port
  -msgChannel|--msgChannel <channel>      NATS Channel
  ```
