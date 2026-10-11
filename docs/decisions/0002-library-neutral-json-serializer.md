# 0002. JSON serialization goes through a library-neutral interface

Status: accepted

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
- The convenience extensions `ToJson` and `FromJson` in `FEx.Json` (and the `JToken`, stream and file helpers built on them) are the Newtonsoft.Json path by design, not an unfinished migration. They always use Newtonsoft.Json, whatever module a consumer registers; see the next section.
- Each converter has to be maintained in both libraries.

## Where Newtonsoft.Json stays

New library-neutral code goes through `IFExJsonSerializer`. The `ToJson` and `FromJson` extensions in `FEx.Json` are the Newtonsoft.Json path by design: they are typed on `JsonSerializerSettings` and `Formatting`, and they are not routed through the registered module. Routing them through an ambient default serializer would silently change behavior at every existing call site, and an overload taking `IFExJsonSerializer` would not stop a consumer who registered System.Text.Json from reaching Newtonsoft.Json through the old ones, so neither is added. The extensions carry no `[Obsolete]`: the in-repo callers use Newtonsoft.Json on purpose, and a warning on them would be noise.

`FEx.SecureStorage`, `FEx.Encryption` and `SecureNotifyPropertyChanged` deliberately stay on Newtonsoft.Json, for two reasons:

- **Plaintext stays out of the JSON.** `SecureNotifyPropertyChanged` carries a Newtonsoft.Json `[JsonConverter]` (`SecureNotifyPropertyChangedConverter`) that makes a serializer write the ciphertext instead of the decrypted value its getters return. Another serializer ignores that converter and writes the plaintext.
- **Persisted secrets keep their format.** The values `FEx.SecureStorage` has already stored (all four platform services serialize with `ToJson` and read back with `FromJson`) are in Newtonsoft.Json's on-disk format, and a different serializer is not guaranteed to read them back.
