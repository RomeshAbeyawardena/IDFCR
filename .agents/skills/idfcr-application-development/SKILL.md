---
name: idfcr-application-development
description: Build, extend, review, or migrate .NET applications that use IDFCR packages. Use when selecting IDFCR components, following its result/handler/persistence conventions, wiring dependency injection, or avoiding duplicate infrastructure in an IDFCR-based application.
---

# IDFCR Application Development

Reuse the application's installed IDFCR capabilities and established local patterns before adding custom infrastructure or another framework.

## Establish the local IDFCR shape

Before designing or editing:

1. Inspect `*.csproj`, central package management files, startup/host registration, and representative nearby features.
2. Identify the installed IDFCR package versions and whether dependencies are NuGet packages or project references.
3. Search for the concrete APIs and extension methods available in that version. Prefer local source and existing compiling usage over remembered signatures.
4. Trace one similar operation end to end: transport → request → handler → repository/service → result → transport mapping.

Do not assume every IDFCR application uses MediatR, EF Core, interceptors, outbox, caching, gRPC, or AI packages. Select only the capabilities relevant to the task.

## Preserve architectural invariants

- Represent expected outcomes with the appropriate `IUnitResult*` contract and `FailureReason`; do not introduce exception-driven control flow for ordinary not-found, validation, or conflict outcomes.
- Keep business intent in requests, handlers, and domain/application services. Keep HTTP, gRPC, CLI, and other protocol translation at their boundaries.
- When the application uses `IUnitOfWorkRequest` and the unit-of-work post-processor, do not call `SaveChangesAsync` inside handlers.
- Route entity changes through repositories so mapping, change detection, interceptors, audit behavior, and outbox behavior remain intact.
- Treat audit fields as interceptor-owned when audit interceptors are registered. Do not manually stamp or overwrite them in handlers or transport models.
- Use IDFCR filters and paging contracts when they are already installed instead of duplicating query parsing and pagination.
- Preserve the domain/persistence separation expressed by `TCommon`, `TDb`, and domain models. Map at the existing boundaries.
- Prefer the smallest package set that completes the feature.

## Choose references progressively

- Read [architecture-and-packages.md](references/architecture-and-packages.md) when selecting packages, locating responsibilities, or understanding request and persistence flow.
- Read [implementation-playbook.md](references/implementation-playbook.md) when implementing or reviewing a feature, registration, persistence behavior, or tests.

If the IDFCR source repository and its `docs/` directory are available, treat those version-matched docs as authoritative. Use this skill's references as a compact guide, then verify public API signatures in the local version.

## Deliver changes

Follow the nearest existing feature's naming, folder structure, and registration style. Test observable application behavior and result semantics, including relevant failure paths. Report which existing IDFCR components were reused and any capability that had to remain application-specific.
