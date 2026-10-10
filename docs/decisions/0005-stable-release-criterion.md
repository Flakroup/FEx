# 0005. Criterion for a stable release

Status: accepted

## Context

FEx is consumed as a submodule and as NuGet packages. A stable release is a promise to consumers about the public API and about behavior, so "ready" needs a definition that does not depend on how finished the work feels.

## Decision

`develop` is merged to `main` and released as stable when all of the following hold:

1. No open issue carries the label `blocks-main`.
2. `dotnet list package --vulnerable --include-transitive` reports no vulnerable package.
3. CI is green on `develop`, including the 100% coverage gate and inspection (see [architecture](../architecture.md#build-and-quality-gates)).

The label `blocks-main` marks an issue that changes the public API or consumer-visible behavior, because those changes should land before a stable version commits to them. Documentation, build and test work does not get the label and never blocks a release.

Issues that were consumer-visible and therefore decided before the first stable release include:

- The HTTP outbox moves a request to the dead-letter state when the server answers with a permanent client error (4xx other than 401, 408 and 429). Server failures count toward a limit (`DefaultMaxServerFailures`, 10), and unavailability answers or redirects count toward another (`DefaultMaxUnavailableAnswers`, 100). 401, 429 and transport failures never count.
- The FTP client moved to FluentFTP.

Other work, such as the analyzer cleanup, the CI duration and the `AuthenticatedUserInfo` type, is tracked as ordinary issues and does not block.

## Consequences

- Whether a release is allowed is a query (open `blocks-main` issues, the vulnerability report, CI status), not a discussion.
- Breaking changes are batched before a stable version and recorded under `Breaking changes` in the changelog.
- Adding the label is a deliberate act: an issue that should block a release has to be labeled.
