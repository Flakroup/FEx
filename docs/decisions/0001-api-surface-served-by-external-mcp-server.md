# 0001. The API surface is served by an external MCP server

Status: accepted

## Context

FEx has more than 60 projects, and consumers and tooling need a fast, accurate view of their public types without opening every source file. Two models were considered:

- **A. An external server that scans the source on demand.** A Model Context Protocol (MCP) server reads the repository with Roslyn and answers queries (search, per-project API, dependencies).
- **B. A per-solution analyzer that emits a generated file at build time.** Every repository that consumes FEx would carry the analyzer and commit or publish the generated API file.

Model B puts a generator into every consumer's build, produces a committed artifact that can drift from the source, and has to be wired up again for each new repository.

## Decision

The API surface is served by the external ApiSurfaceMcp server (model A). It scans a repository's source with Roslyn when asked and caches the result keyed by the repository path and its git HEAD, so changed source is picked up automatically. Its tools include `list_repos`, `list_projects`, `search_api`, `get_project_api` and `refresh`.

FEx contains no API generator and no committed API-surface file. A repository that wants to be queried is registered with the server by path.

## Consequences

- Nothing about the API surface has to be generated, committed or kept in sync in this repository, and it cannot drift from the source.
- The first query after a change pays for a re-scan; `refresh` forces one.
- Answers depend on the server being available. The source itself stays the authority when the server is not.
- Tools that previously read a generated API file must query the server instead.
