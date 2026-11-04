# Spike: explicit saga timeout persister

- Targets NServiceBus PR 8002 at `f23189533fca92f35463775cd802cb38c1981170`, consumed as the local package version `10.3.0-alpha.timeouts-explicit.f2318953` from `nugets/` (see `nuget.config`).
- Timeouts live in a new, separate `<prefix>SagaTimeoutData` table (`<prefix>ST` on Oracle, within the 30-character limit): one row per timeout, keyed by `SagaId` + `TimeoutId`, with the `StoredSagaTimeout` serialized as JSON in `Data`. Completing a saga deletes all rows of that saga in the same transaction.
- The table is created by `SagaTimeout_Create.sql` (dropped by `SagaTimeout_Drop.sql`) through saga installation. These scripts are generated whenever `ProduceSagaScripts` is true, even when `ProduceTimeoutScripts = false`, because Core loads the timeout table for every existing saga.
- The legacy timeout-manager table (`<prefix>TimeoutData`, `<prefix>TO` on Oracle) and its `Timeout_*.sql` scripts are unchanged. The legacy table may coexist with the new table and is never modified, installed, or dropped by saga timeouts.
- No migration is provided from saga metadata or the earlier spike table; the new saga-timeout table starts empty.
- The spike packages in `nugets/` are local, ignored build artifacts, not published dependencies. To build this branch elsewhere, first build and pack the `NServiceBus`, `NServiceBus.AcceptanceTesting`, `NServiceBus.AcceptanceTests.Sources`, and `NServiceBus.PersistenceTests.Sources` packages from the commit above with `MinVerVersionOverride` and `Version` set to the package version above.
- Building locally may need `-p:NuGetAudit=false` because of the existing transitive `System.Security.Cryptography.Pkcs` advisories. This bypasses the audit gate; it does not resolve the advisories.
