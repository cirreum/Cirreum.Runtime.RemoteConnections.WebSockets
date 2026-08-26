# Changelog

All notable changes to **Cirreum.Runtime.RemoteConnections.WebSockets** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

## [2.0.1] - 2026-08-26

### Fixed

* **A connection is registered scoped in a browser, and a singleton on a server.** It was always a
  singleton, which is right on a server and wrong in a browser: a browser has exactly one scope,
  created by the host and living as long as the application, and every service the application
  composes is registered against it — the access-token provider among them. A singleton lives in the
  root container instead, where none of them can be reached.

  The effect depended on whether the host validated scopes: either the connection threw when it
  resolved the credential source, or it silently resolved a *second* copy of the whole graph out of
  the root, distinct from the one the application uses. Both were reported from the field, and the
  workaround was to hand-roll a scope inside every credential source.

  `AddRemoteConnectionFactory` carried the same fault and is fixed with it. The change reaches more
  than credentials: a connection is built with `ActivatorUtilities`, so any scoped service an
  application injects into its own connection type was equally unreachable.

  Nothing observable changes for a consumer. A browser scope lives as long as the application, so
  there is still exactly one connection for the application's life; on a server the registration is
  unchanged.

## [2.0.0] - 2026-08-26

### Breaking

* **The connection types move to `Cirreum.RemoteServices.Connections`**, following
  `Cirreum.Contracts` 5.0.0, `Cirreum.Domain` 5.0.0 and `Cirreum.RemoteConnections.WebSockets`
  2.0.0. A service is something you call; a connection is something you hold open, so it nests
  rather than sitting alongside. An application writing a connection type changes one `using`.

* **The credential seam follows the transport.** `RemoteConnectionOptions.CredentialProvider`
  replaces `AccessTokenProvider`, and the ambient source is `IRemoteConnectionCredentialSource`.
  A resolved credential that is `null` now fails the attempt rather than opening an
  unauthenticated socket; `AuthorizationHeaderSettings.None` is how a connection says it wants
  none.

### Added

* **`RemoteConnectionOptions.Scopes` is carried through registration**, so a connection names the
  audience its credential is minted for and the host runtime needs no per-application source.

* **The registration stamps the connection type**, which reaches the credential source in its
  request. A source registered keyed to that type is preferred over the unkeyed one — so a bridge
  holding one socket to a provider and another to its own backend can give each its own mechanism.

### Fixed

* **A factory-created connection carries the registered scopes.** The per-instance options copy is
  the only path a per-session connection's options travel, and a property it omits is lost in
  silence — for scopes, that is a connection whose credential source is never told which audience
  to mint for. This package's per-session verb is the common one, so the omission would have
  reached most of its consumers.

### Updated

- `Cirreum.RemoteConnections.WebSockets` 2.0.0.

### Updated

- Updated NuGet packages.

## [1.0.0] - 2026-08-25

Initial release of **Cirreum.Runtime.RemoteConnections.WebSockets**.

### Added

* **`AddRemoteConnectionFactory<TConnection>()`** — registers
  `IRemoteConnectionFactory<TConnection>` for connections belonging to a session rather than to the
  application: a telephony bridge holds one outbound connection per active call. It registers no
  connection instance and no `IRemoteConnection` forwarding, so a status surface enumerating
  standing connections never sees per-session ones, and the caller owns what it creates. `Create`
  optionally adjusts the registered options for one session — a different deployment, a per-call
  subprotocol — leaving the registration untouched for every later one.
* **`AddRemoteConnection<TConnection>()`** — registers a typed WebSocket connection living for the
  lifetime of the application. The connection resolves as `TConnection` and as `IRemoteConnection`,
  and the container disposes it with the host.
* **One registration per connection type**, in either shape. Registering the same type twice with
  equal options is a no-op; with different options, or under both verbs, it throws. Subclassing the
  connection reaches a second endpoint. The registry is keyed by service collection rather than held
  process-wide, so a second container in the same process — a test host, a second builder — composes
  its own registrations rather than silently skipping them.
* **Options are validated as they are registered**, so a missing or relative endpoint surfaces while
  the application composes rather than when something first resolves the connection.
* **`configureTransport`** exposes `ClientWebSocketOptions`, applied after the framework has
  configured each socket, so any transport setting can be overridden — the place to offer
  subprotocols.

Both verbs are extension members on `IDomainApplicationBuilder`, so the package serves a Blazor
WebAssembly client and a server-side host without a per-host variant.
