# Feature Specification: Multiplayer Shooting System

**Feature Branch**: `001-multiplayer-shooting`  
**Created**: 2026-03-24 (Pivoted: 2026-04-10)  
**Status**: Draft  
**Input**: User description: "El sistema de disparo multijugador cooperativo de Arena Bots: Hordes. Los jugadores pueden disparar proyectiles en cualquier dirección para abatir oleadas de zombies-bots manejados por la IA del servidor. Los eventos de disparo se sincronizan en tiempo real..."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real-time Shooting (Priority: P1)

As a player, I want to be able to fire projectiles in any direction and see them appear immediately on my screen, so that the game feels responsive.

**Why this priority**: Core gameplay mechanic. Without shooting, the game's primary interaction is missing.

**Independent Test**: Can be fully tested by pressing the fire button in a single-player or local context and observing the projectile spawning and moving in the intended direction.

**Acceptance Scenarios**:

1. **Given** the player is alive and in an active match, **When** the player presses the fire button, **Then** a projectile is spawned immediately at the player's position and moves in the direction of the aim.
2. **Given** the player fires a projectile, **When** the projectile is spawned, **Then** a WebSocket event is sent to the server with the correct position, direction, and timestamp.

---

### User Story 2 - Multiplayer Synchronization (Priority: P2)

As a player, I want to see the projectiles fired by other players in the same match, so that I can react to their actions in real-time.

**Why this priority**: Essential for the multiplayer experience. Ensures all players share the same game state.

**Independent Test**: Connect two clients to the same server; fire from Client A and verify the projectile appears and moves correctly on Client B.

**Acceptance Scenarios**:

1. **Given** two players are in the same session, **When** Player A fires, **Then** Player B receives a `disparo_retransmision` event and spawns the projectile at the received position and direction.

---

### User Story 3 - Server Validation (Priority: P3)

As a developer, I want the server to validate all firing events to prevent cheating and ensure fair play.

**Why this priority**: Prevents common exploits like rapid fire or firing while dead.

**Independent Test**: Send forged or invalid firing events from a custom client and verify the server rejects them.

**Acceptance Scenarios**:

1. **Given** a player is dead, **When** the server receives a firing event from that player, **Then** the server rejects the event and does not retransmit it.
2. **Given** a player fires faster than 10 times per second, **When** the server receives the 11th event in a second, **Then** the server ignores or rejects the event (rate limiting).

---

### Edge Cases

- **Duplicate Events**: What happens when a client sends the same event twice due to network jitter? The server MUST implement idempotency checks using the client-provided `timestamp` to ignore duplicate events within a 100ms window.
- **Invalid Direction**: How does the system handle a non-normalized direction vector? The server MUST reject events where the direction vector magnitude is outside the range [0.95, 1.05].
- **Position Out of Bounds**: What happens if a player fires from a position outside the map limits? The server MUST reject events with coordinates outside the defined arena boundaries.
- **Client Disconnection**: If a client disconnects after firing but before retransmission, the projectile should still persist for other clients until it hits a target or exits the arena.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow players to fire projectiles in any 360-degree direction.
- **FR-002**: The Unity client MUST implement client-side prediction, spawning the projectile locally before receiving server confirmation.
- **FR-003**: The server MUST be the source of truth, validating all firing events (player state, rate limits, position, direction).
- **FR-004**: The server MUST retransmit validated firing events to all clients in the session with a latency target of <50ms.
- **FR-005**: All network payloads MUST use exactly 2 decimal precision for coordinates and normalized vectors.
- **FR-006**: The server MUST implement idempotency for firing events based on player ID and timestamp.

### WebSocket Events *(mandatory for real-time)*

**Event Name**: `disparo` (Client -> Server)
**Payload Schema**:
```json
{
  "tipo": "disparo",
  "jugadorId": "string",
  "partidaId": "string",
  "posicion": { "x": "number (float, 2 decimals)", "y": "number (float, 2 decimals)" },
  "direccion": { "x": "number (float, 2 decimals)", "y": "number (float, 2 decimals)" },
  "timestamp": "number (long, unix ms)"
}
```

**Event Name**: `disparo_retransmision` (Server -> Client)
**Payload Schema**:
```json
{
  "tipo": "disparo_retransmision",
  "jugadorId": "string",
  "posicion": { "x": "number (float, 2 decimals)", "y": "number (float, 2 decimals)" },
  "direccion": { "x": "number (float, 2 decimals)", "y": "number (float, 2 decimals)" },
  "proyectilId": "string"
}
```

### Key Entities *(include if feature involves data)*

- **Projectile**: Represents the fired object. Attributes: `id`, `ownerId`, `startPosition`, `direction`, `speed`, `spawnTimestamp`.
- **Player**: The actor firing. Attributes: `id`, `isAlive`, `lastFireTimestamp`, `fireRateCount`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Local projectiles appear on the firing client within 16ms (1 frame at 60fps) of the input.
- **SC-002**: Remote projectiles appear on other clients within 100ms of the original fire event (assuming <50ms network latency).
- **SC-003**: Server successfully filters out 100% of firing attempts from players marked as "dead".
- **SC-004**: System handles up to 10 firing events per second per player without dropping valid events.
- **SC-005**: All projectile trajectories are synchronized across all clients with a spatial deviation of less than 0.1 units.
