# Quickstart: Multiplayer Shooting System

## Development Environment Setup
1. Backend: `npm install` in `/backend`.
2. Unity: Open `unity-client` in Unity 2022.3 LTS.
3. Configuration: Ensure Unity and Node.js use the same WebSocket port (default: 3000).

## Running the System

### 1. Start the Backend
```bash
cd backend
npm install
node src/index.js
```

### 2. Run Unity Client
1. Open Unity Project in `unity-client/`.
2. Add `NetworkManager`, `ShootController`, and `RemoteProjectileSpawner` to a GameObject.
3. Assign a Projectile Prefab (with `Projectile.cs` attached) to `ShootController` and `RemoteProjectileSpawner`.
4. Press Play.

## Verification Scenarios

### V-001: Local Fire Prediction
1. Launch Unity Client.
2. Enter the arena.
3. Click to fire.
4. **Outcome**: Projectile spawns and moves instantly.

### V-002: Server Rate Limiting
1. Use a modified client to send 15 `disparo` events in 1 second.
2. Monitor Node.js logs.
3. **Outcome**: Events 11-15 are rejected.

### V-003: Synchronization
1. Launch two Unity clients.
2. Player A fires.
3. **Outcome**: Player B sees Player A's projectile moving with consistent speed and direction.
