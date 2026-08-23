# Cirreum.Runtime.RemoteConnections.WebSockets 1.0.0

First release of the app-facing registration surface for Cirreum raw WebSocket remote connections.

One builder line registers a typed connection with framework-owned receive and reconnect loops, token refresh, state, and disposal. `AddRemoteConnectionFactory` is the shape for per-session connections — one per phone call, one per bridge — where a singleton would be wrong.
