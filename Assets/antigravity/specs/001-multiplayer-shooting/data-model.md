# Data Model: Multiplayer Shooting System

## Entities

### Projectile (Unity/Node.js)
Represents a projectile in flight.
- **id** (string): Unique identifier.
- **ownerId** (string): ID of the player who fired.
- **startPosition** (Vector2): Initial (x, y) rounded to 2 decimals.
- **direction** (Vector2): Normalized (magnitude 1.0 ±0.05).
- **speed** (float): Constant velocity units/sec.
- **spawnTimestamp** (long): Creation time (unix ms).

### PlayerSession (Node.js)
Tracks player state for validation.
- **playerId** (string): Unique ID.
- **isAlive** (boolean): Current gameplay state.
- **lastFireTimestamps** (Array<long>): Rolling cache of last 10 firing times for rate limiting and idempotency.

## Validation Rules
- **Magnitude**: `sqrt(x^2 + y^2)` MUST be `1.0 ±0.05`.
- **Precision**: Payload floats MUST have at most 2 decimal places.
- **Rate Limit**: Max 10 shots per 1000ms per player.
- **State**: `isAlive` MUST be true.
