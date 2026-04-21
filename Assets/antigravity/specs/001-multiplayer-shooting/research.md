# Research: Multiplayer Shooting System

## Core Technologies

### Unity WebSocket Integration
- **Decision**: Use `NativeWebSocket` for Unity.
- **Rationale**: Provides cross-platform WebSocket support including WebGL.
- **Best Practices**:
  - Use `MainThreadDispatcher` to handle WebSocket callbacks on the Unity main thread.
  - Implement periodic heartbeats to maintain the connection.

### Node.js WebSocket Handler
- **Decision**: Use `ws` library with `Express`.
- **Rationale**: Lightweight and industry standard for Node.js WebSocket servers.
- **Best Practices**:
  - Use a centralized event router for WebSocket messages.
  - Implement robust error handling for malformed JSON payloads.

## Implementation Patterns

### Client-Side Prediction & Reconciliation
- **Decision**: Spawn projectile locally on Input, then tag with a client-generated ID or timestamp.
- **Rationale**: Eliminates perceived latency.
- **Alternatives Considered**: Waiting for server response (rejected: poor UX).

### Vector Normalization & Precision
- **Decision**: Enforce magnitude 1.0 ±0.05 on the server. Round all coordinates to 2 decimal places before sending.
- **Rationale**: Aligns with Constitution Principle IV and reduces payload size.

### Server Idempotency
- **Decision**: Cache the last 10 firing timestamps per player.
- **Rationale**: Prevents accidental duplication due to network retries.
