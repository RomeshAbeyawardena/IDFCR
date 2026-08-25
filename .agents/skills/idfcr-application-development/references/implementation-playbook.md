# IDFCR implementation playbook

Use this playbook after inspecting the application's actual dependencies and a nearby working feature.

## Feature discovery checklist

Search for:

- `PackageReference` and `ProjectReference` entries beginning with `IDFCR`.
- `AddMediatorServicesAndPipelines`, `ConfigureExceptionBehaviourManager`, `AddInterceptors`, `ScanFilters`, and transport registration calls.
- Request interfaces such as `IUnitResultRequest<T>` and `IUnitOfWorkRequest`.
- Existing handlers, validators, repositories, filters, mapper implementations, and `.AsHttp()` boundaries.
- Application-specific wrappers around IDFCR contracts; preserve them when they are deliberate conventions.

Build a small reuse map before coding:

| Concern | Existing component | Planned reuse or justified exception |
|---|---|---|
| Result contract | | |
| Request/handler | | |
| Validation | | |
| Persistence/UoW | | |
| Mapping | | |
| Filtering/paging | | |
| Interceptors/outbox | | |
| Transport mapping | | |

## Results and handlers

Choose the result shape that matches the operation: scalar, collection, paged, or chained. Preserve `FailureReason`, `UnitAction`, metadata, and original/modified state when they carry meaningful behavior.

Expected failures should be returned through `UnitResult` factories. Let the configured exception pipeline convert genuinely unexpected handler exceptions; do not catch broad exceptions merely to hide them or return an unclassified success/failure.

When adding a handler:

1. Follow the local request and handler interfaces.
2. Add a FluentValidation validator if the pipeline is enabled and the input has rules.
3. Coordinate existing repositories/services rather than embedding persistence or transport logic.
4. If the request should commit and the UoW post-processor is enabled, implement the local `IUnitOfWorkRequest` convention and leave committing to the pipeline.

## Persistence, mapping, and filters

`RepositoryBase<TCommon, TDb, T, TKey>` separates a shared contract, persistence model, domain/transport model, and key. Do not collapse those roles solely to reduce mapping code when the surrounding application maintains the separation.

For updates, call the repository's operation rather than directly attaching or mutating EF entities outside the established repository. This preserves change detection and interceptor execution.

Use the filter factory and existing `FilterBase`/paged-filter conventions for reusable queries. Ensure the relevant assemblies are passed to `ScanFilters`; a correct filter class that is not scanned is effectively absent.

## Interceptors and auditing

Inspect interceptor registration before implementing cross-cutting entity logic. Built-in audit interceptors populate creation timestamps during inserts and modification timestamps during updates for the corresponding metadata interfaces.

Do not set audit timestamps in handlers, controllers, mapped input DTOs, or repositories unless the application explicitly replaces the interceptor behavior. Properties marked to be ignored during apply operations must remain interceptor-owned.

Use custom interceptors for entity lifecycle behavior that must run consistently regardless of which handler initiated persistence. Keep ordinary business rules in domain/application code.

`IScopedResources` is an execution-scoped handoff mechanism for already-resolved objects and staged pipeline state. Do not use it as a general service locator.

## Registration

Prefer the package's registration extension over manually registering its internal services. Preserve ordering constraints found in the application; notably, exception behavior configuration may need to occur before mediator pipeline registration.

Pass the assemblies containing application handlers, validators, filters, interceptors, or processors to the relevant scanning method. When discovery fails, verify the scanned assembly before adding duplicate explicit registrations.

## Testing

Match test scope to the behavior:

- Handler tests: construct the handler and assert result semantics and collaborator calls.
- Validator tests: test rules independently.
- Pipeline tests: resolve MediatR through a real `ServiceProvider` when validation, exception conversion, or UoW behavior matters.
- Repository tests: cover mapping, not-found, updates, no-change conflicts, paging, audit fields, and interceptor effects relevant to the change.
- Interceptor tests: construct the interceptor/factory with a deterministic `TimeProvider` or other controlled dependency.
- Transport tests: assert result-to-protocol mapping at the boundary.

Assert public behavior (`IsSuccess`, `FailureReason`, `Action`, returned data and side effects), not private concrete result implementation types.

Run the narrow affected test project first, then expand to the solution or dependent projects in proportion to the change.

## Review warnings

Flag implementations that:

- add another result wrapper over `IUnitResult*` without a boundary requirement;
- call `SaveChangesAsync` inside a handler already governed by the UoW pipeline;
- manually stamp interceptor-owned audit properties;
- bypass repositories for an operation that relies on interceptors or outbox staging;
- duplicate filter/paging logic already provided by IDFCR;
- install broad packages for one small capability;
- invent API signatures without checking the resolved IDFCR version.
