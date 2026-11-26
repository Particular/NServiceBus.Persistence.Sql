# Engineering context

## About this repository

NServiceBus.Persistence.Sql provides NServiceBus persistence using SQL databases. For local build and test steps see the `README.md`.

- `src/` — SQL persistence implementations, tests, and supporting projects
- `.github/workflows/` — CI pipelines

## Start here

- [SQL Persistence documentation](https://docs.particular.net/persistence/sql/) — public usage and configuration documentation
- [README.md](../README.md) — how to build and test locally
- [Contributing](https://docs.particular.net/platform/contributing) — contribution process

## Architecture and design

- [Saga persistence](https://docs.particular.net/persistence/sql/saga) — how the persister stores saga state and handles correlation and concurrency

## Upstream contracts

- [NServiceBus: for downstream components](https://github.com/Particular/NServiceBus/blob/master/docs/README.md#for-downstream-components) — the index of Core contracts for persisters and other downstream components

## Decisions and rationale

- [Architecture and design decisions](decisions/)

### Decisions recorded in pull requests

- Bound pooled saga serialization buffers and pass only the serialized data to SqlClient so a large saga does not inflate later small writes — [#2180](https://github.com/Particular/NServiceBus.Persistence.Sql/pull/2180)
- Why outbox operations are serialized with System.Text.Json before sagas, which stored-text differences are accepted, and why mixed-version endpoints keep working — [#2206](https://github.com/Particular/NServiceBus.Persistence.Sql/pull/2206)

Keep this index current when a canonical source is added, replaced, or retired; link, do not copy.
