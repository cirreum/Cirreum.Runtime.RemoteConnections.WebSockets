# Cirreum.Runtime.RemoteConnections.WebSockets

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.RemoteConnections.WebSockets.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.RemoteConnections.WebSockets/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.RemoteConnections.WebSockets.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.RemoteConnections.WebSockets/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.RemoteConnections.WebSockets?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.RemoteConnections.WebSockets/releases)
[![License](https://img.shields.io/badge/license-MIT-F2F2F2?style=flat-square&labelColor=1F1F1F)](https://github.com/cirreum/Cirreum.Runtime.RemoteConnections.WebSockets/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**App-facing registration for Cirreum raw WebSocket remote connections**

## Overview

**Cirreum.Runtime.RemoteConnections.WebSockets** registers typed raw WebSocket client connections on a Cirreum application builder.

```csharp
builder.AddRemoteConnectionFactory<RealtimeVoiceConnection>(options => {
    options.EndpointUri = new Uri("wss://provider.example.com/realtime");
});
```

Use `AddRemoteConnectionFactory<TConnection>()` when a connection belongs to a session — one per
phone call, one per bridge — and `AddRemoteConnection<TConnection>()` when a single connection lives
for the lifetime of the application. Either way the framework owns receive and reconnect loops, token
refresh, observable state, and disposal.

The transport implementation ships in `Cirreum.RemoteConnections.WebSockets` and flows in
transitively.

## Documentation

- [CHANGELOG](docs/CHANGELOG.md)
- [Backlog](docs/BACKLOG.md)

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Runtime.RemoteConnections.WebSockets follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*