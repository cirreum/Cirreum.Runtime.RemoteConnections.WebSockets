# Cirreum.Runtime.RemoteConnections.WebSockets 2.0.0 — the registration names the connection

## Why this release exists

The credential seam beneath this package changed. A credential source used to take no parameters, so
one registration answered identically for every connection an application opened — and where the
host could not infer an audience it supplied its own defaults. For a bridge holding several sockets
to several places, that is not a near miss; it is one answer for questions that have different
answers.

The fix is that a source is told which connection it is supplying for. This package is where that
information exists: it holds `TConnection`, and the transport beneath it does not.

## What's new

**A connection names its audience:**

```csharp
builder.AddRemoteConnectionFactory<RealtimeVoiceConnection>(options => {
    options.EndpointUri = new Uri("wss://provider.example.com/realtime");
    options.Scopes = ["api://contoso/access_as_user"];
});
```

**The registration stamps the connection type**, and a source registered keyed to that type is
preferred over the unkeyed one:

```csharp
services.AddKeyedScoped<IRemoteConnectionCredentialSource, ProviderCredentialSource>(typeof(RealtimeVoiceConnection));
```

That matters more here than on the SignalR side. The founding consumer for raw WebSockets is a
telephony bridge holding one socket to a realtime provider and another to its own backend — two
mechanisms, two identity providers, in one process. Before this, both got whatever the single
ambient source returned.

## Fixed

**A factory-created connection carries the registered scopes.** The factory copies the registered
options per instance, and that copy is the only path a per-session connection's options travel. It
omitted `Scopes`, which would have left every per-session connection's credential source with no
audience to mint for — silently, since a forgotten property looks exactly like one that was never
set.

The per-session verb is this package's common shape, so the omission would have reached most of its
consumers. Caught by a test written for the copy rather than for the feature.

## Compatibility

- **The registration verbs are unchanged**, including their overloads, the
  one-registration-per-connection-type rule, registration-time validation, and options-equality
  dedup.
- **One `using` changes** where an application writes a connection type: the connection types moved
  to `Cirreum.RemoteServices.Connections`.
- **Declare `Scopes` on connections whose credential comes from the host's session.** A connection
  carrying a provider key in `AuthorizationHeader` or a `CredentialProvider` never consults the
  ambient source and is unaffected. See [MIGRATION-v2.md](MIGRATION-v2.md).

## See also

- `Cirreum.RemoteConnections.WebSockets` 2.0.0 — the transport that resolves the source, and where
  any authorization scheme may be resolved per attempt rather than only Bearer.
- `Cirreum.Contracts` 5.0.0 — the credential contract and the reasoning behind its shape.
