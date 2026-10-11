# 0002. JSON serialization goes through a library-neutral interface

Status: accepted, partly implemented

## Context

FEx started on Newtonsoft.Json. Newer targets and consumers prefer System.Text.Json, and a framework library should not force one serializer on every application. Migrating everything in one step would break every consumer at once, and the two libraries differ in behavior (converters, null handling, case rules), so a blind swap changes data on the wire.

## Decision

Serialization is abstracted behind `IFExJsonSerializer` in `FEx.Json.Abstractions`. Two implementations are shipped and registered by their own modules:

- `FExNewtonsoftJsonSerializer` in `FEx.Json`.
- `FExSystemTextJsonSerializer` in `FEx.Json.SystemTextJsonx`.

Libraries built on top of serialization default to System.Text.Json and keep Newtonsoft.Json as an opt-in package. `FEx.Flurlx` follows this: its default serializer is System.Text.Json and `FEx.Flurlx.Newtonsoft` switches it. The tolerant string-to-number converters (`ParseStringToDoubleConverter`, `ParseStringToLongConverter`) exist for both libraries so that behavior stays equal when a consumer switches.

## Consequences

- Applications choose a serializer by registering one module instead of by changing code.
- New code takes `IFExJsonSerializer` and does not call `JsonConvert` directly.
- The migration is incomplete. The convenience extensions `ToJson` and `FromJson` in `FEx.Json` still call Newtonsoft.Json directly rather than the interface, so they are not affected by the module a consumer registers. Moving them behind the interface is the remaining step.
- Each converter has to be maintained in both libraries.
