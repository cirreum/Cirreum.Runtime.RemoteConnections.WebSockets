# Cirreum.Runtime.RemoteConnections.WebSockets v1 → v2 Migration

v2 follows `Cirreum.Contracts` 5.0.0, `Cirreum.Domain` 5.0.0 and
`Cirreum.RemoteConnections.WebSockets` 2.0.0. The registration verbs are unchanged; what moves is
underneath them.

---

## 1. Namespace

| v1 | v2 |
| --- | --- |
| `using Cirreum.RemoteServices;` | `using Cirreum.RemoteServices.Connections;` |

The connection types moved. `AuthorizationHeaderSettings` and `RemoteIdentityConstants` did not — a
file touching both imports both namespaces.

A remote service is something you *call*; a remote connection is something you *hold open*. The
second is a relationship with a remote service rather than a peer of one, so it nests.

`AddRemoteConnection<TConnection>` and `AddRemoteConnectionFactory<TConnection>` are extension
members on `IDomainApplicationBuilder` in `Cirreum.Runtime`, as before.

## 2. The credential seam

| v1 | v2 |
| --- | --- |
| `options.AccessTokenProvider` | `options.CredentialProvider` |
| `Func<CancellationToken, ValueTask<string?>>` | `Func<CancellationToken, ValueTask<AuthorizationHeaderSettings?>>` |
| `IRemoteConnectionTokenSource` | `IRemoteConnectionCredentialSource` |

The full before/after is in `Cirreum.Contracts`' `MIGRATION-v5.md` — one guide for the whole track.

## 3. ⚠️ Declare each connection's audience

**Behavioural, not a compile error.**

A connection now names the audience its credential is minted for:

```csharp
builder.AddRemoteConnectionFactory<RealtimeVoiceConnection>(options => {
    options.EndpointUri = new Uri("wss://provider.example.com/realtime");
    options.Scopes = ["api://contoso/access_as_user"];
});
```

Where the host's credential source cannot mint without an audience, a connection that declares no
scopes now receives no credential and fails at connect rather than presenting one for an audience
nobody asked for.

A connection whose credential is a provider key rather than a session token is unaffected — set
`options.AuthorizationHeader`, or supply a `CredentialProvider`, and the ambient source is never
consulted.

## New capabilities

### A credential source per connection

```csharp
services.AddKeyedScoped<IRemoteConnectionCredentialSource, ProviderCredentialSource>(typeof(RealtimeVoiceConnection));
```

The registration stamps the connection type into the credential request, and a source registered
keyed to that type is preferred over the unkeyed one. A telephony bridge holding one socket to a
provider and another to its own backend gives each its own mechanism, without either connection
knowing about the other.

### Any scheme, resolved per attempt

The transport beneath this package resolves any authorization scheme per connect attempt, not only
Bearer — a fresh socket is built per attempt and its headers are set after the credential resolves.
An ApiKey therefore refreshes across reconnects exactly as a token does.

## Fixed

A factory-created connection now carries the registered `Scopes`. The per-instance options copy is
the only path a per-session connection's options travel; a property it omits is lost in silence.
The per-session verb is this package's common one, so the omission would have reached most of its
consumers.

## What didn't change

- Both registration verbs, their overloads, and the one-registration-per-connection-type rule.
- Registration-time options validation, and options-equality dedup.
- `IRemoteConnection` forwarding for `AddRemoteConnection`, and its deliberate absence for
  `AddRemoteConnectionFactory` — a status surface enumerating standing connections must not see
  per-session ones.
- The `configureTransport` escape hatch, which still receives `ClientWebSocketOptions` per attempt.
