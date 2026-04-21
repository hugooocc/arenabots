# Implementation Plan: Multiplayer Shooting System

**Branch**: `001-multiplayer-shooting` | **Date**: 2026-03-24 | **Spec**: [specs/001-multiplayer-shooting/spec.md]
**Input**: Feature specification from `/specs/001-multiplayer-shooting/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Implement a real-time multiplayer shooting system for Arena Bots. The feature involves creating a WebSocket-based communication flow where the Unity client performs client-side prediction for firing projectiles, while the Node.js backend validates player state, rate limits, and vector precision before retransmitting events to all clients for synchronization.

## Technical Context

**Language/Version**: C# (Unity 2022.3 LTS), JavaScript (Node.js 20 LTS)  
**Primary Dependencies**: NativeWebSocket (Unity), ws (Node.js), Express.js 4.x (Node.js)  
**Storage**: N/A (In-memory session state)  
**Testing**: unit tests (Node.js), integration tests (Unity/Node.js)  
**Target Platform**: Unity Client (Windows/WebGL), Node.js Server  
**Project Type**: Multiplayer Game (Client/Server)  
**Performance Goals**: <50ms server retransmission, <16ms client-side prediction  
**Constraints**: 10 shots/sec rate limit, 2 decimal precision for network payloads, Normalized vectors (1.0 ±0.05)  
**Scale/Scope**: Real-time event synchronization for multiplayer arena matches

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [ ] **Principle I (WebSocket-First)**: Plan uses WebSockets for shooting events. (PASS)
- [ ] **Principle II (Strict Validation)**: Server validates rate limits, player state, and implements idempotency. (PASS)
- [ ] **Principle III (Client-Side Prediction)**: Unity client spawns projectiles immediately. (PASS)
- [ ] **Principle IV (Deterministic Movement)**: Normalized vectors and decimal precision enforced. (PASS)
- [ ] **Principle V (Automated Validation)**: Tests planned for handlers and collision math. (PASS)
- [ ] **Principle VI (AI Traceability)**: Prompt logs will be maintained in the feature directory. (PASS)

## Project Structure

### Documentation (this feature)

```text
specs/001-multiplayer-shooting/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── websocket/       # Shoot handler implementation
└── tests/               # Validation tests

unity-client/
└── Assets/
    └── Scripts/
        └── Shooting/    # Shoot controller and projectile logic
```

**Structure Decision**: Option 2: Web application (frontend/backend) modified for Unity/Node.js structure.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
