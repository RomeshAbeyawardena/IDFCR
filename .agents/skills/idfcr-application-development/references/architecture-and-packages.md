# IDFCR architecture and package selection

IDFCR is an opt-in .NET toolkit. Its central design is a typed operation flow with consistent outcomes, handler conventions, and composable infrastructure registration. Consumer applications retain their domain models and business logic.

## Core flow

```text
HTTP / gRPC / CLI / background caller
    → typed request
    → validation and exception pipelines
    → handler and domain/application services
    → repository, filters, interceptors, and optional outbox
    → IUnitResult*
    → transport-specific mapping
```

Expected failures travel as result state. Transport projects translate those results; they should not redefine application semantics.

With the unit-of-work post-processor enabled, successful requests marked with `IUnitOfWorkRequest` are committed after the handler completes. This allows repository interceptors and staged outbox work to participate in the same application flow.

## Capability map

| Need | Primary packages or package family | Reuse |
|---|---|---|
| Typed outcomes | `IDFCR.Abstractions.Results` | `IUnitResult*`, `UnitResult`, `FailureReason`, `UnitAction` |
| Requests and handlers | `IDFCR.Abstractions.Mediator`, `.Mediator.Extensions` | request/handler contracts, validation, exception and UoW pipelines |
| HTTP boundary | `IDFCR.Results.Http` | `.AsHttp()`, HTTP status mapping |
| Shared model metadata | `IDFCR.Abstractions.Metadata` | identity, audit, paging/order and delta contracts |
| Model mapping | `IDFCR.Abstractions.Mapper`, `.Mapper.Extensions` | mapper contracts, base types, registration |
| Repository contracts | `IDFCR.Abstractions.Persistence` | repositories, unit of work, repository lifecycle |
| EF Core persistence | `IDFCR.Persistence.EntityFrameworkCore`, `.Extensions` | EF repositories/UoW and delta helpers |
| Query composition | `IDFCR.Abstractions.Filters` | filter factory, paged filters, predicate composition and scanning |
| Entity lifecycle | `IDFCR.Abstractions.Interceptors`, `.DependencyInjection` | audit/change interceptors, factory and scanning |
| Reliable delivery | `IDFCR.Abstractions.Outbox*`, `IDFCR.Outbox.*` | staged outbox messages, readers, dispatch and EF support |
| Grouped caching | `IDFCR.Abstractions.Caching`, `IDFCR.Caching*` | cache groups, serialization, HTTP distributed cache |
| gRPC | `IDFCR.Abstractions.GRPC*`, `IDFCR.GRPC.Client.Extensions` | shared contracts, result mapping and service discovery |
| CLI and migrations | `IDFCR.Abstractions.Cli*`, `IDFCR.DatabaseUpdater` | command dispatch and migration host |
| AI providers | `IDFCR.AI.Abstractions`, `.Http`, `.OpenAI` | provider-neutral AI contract and implementations |
| Cryptography | `IDFCR.Abstractions.Cryptography`, `IDFCR.Cryptography*` | password/key and token protection abstractions/implementations |
| Tests | `IDFCR.TestUtilities` | reusable test infrastructure |

Package names and APIs can evolve. Confirm them against the application's resolved dependencies.

## Common package combinations

- Results only: `IDFCR.Abstractions.Results`.
- Handler application: Results + Mediator + Mediator.Extensions.
- Minimal/Web API: handler application + `IDFCR.Results.Http`.
- EF application: Persistence + Filters + Interceptors + interceptor DI + EF Core implementation; add mediator/UoW packages only when that flow is used.
- Outbox: add only the outbox contracts, interceptor, persistence, and publisher/reader implementation needed by the application.

Avoid adding an umbrella dependency merely for convenience. IDFCR packages are deliberately independently adoptable.

## Responsibility boundaries

- Requests express an operation's intent and data.
- Validators reject invalid inputs before business execution.
- Handlers coordinate; domain/application services contain reusable business behavior.
- Repositories own persistence mapping and entity lifecycle invocation.
- Interceptors own cross-cutting entity behavior such as timestamps, auditing, soft deletion, and outbox staging.
- The unit of work owns committing.
- Filters own reusable query composition and paging.
- Transport adapters own protocol status and serialization concerns.
