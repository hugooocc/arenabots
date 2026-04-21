<!--
Sync Impact Report:
- Version change: 1.0.0 → 1.1.0 (Minor: AI traceability & technical refinements)
- List of modified principles:
  - II. Strict Server-Side Validation (Added idempotency/timestamp requirements)
  - IV. Deterministic Movement & Vectors (Added magnitude/precision requirements)
- Added sections:
  - Principle VI: AI-Assisted Development Traceability
- Removed sections: none
- Templates requiring updates (✅ updated / ⚠ pending):
  - .specify/templates/plan-template.md (✅ updated)
  - .specify/templates/spec-template.md (✅ updated)
  - .specify/templates/tasks-template.md (✅ updated)
- Follow-up TODOs: none
-->

# Arena Bots Constitution

## Core Principles

### I. WebSocket-First Communication
All real-time gameplay events (shooting, movement, collisions) MUST use WebSockets for low-latency delivery. HTTP is reserved for non-time-critical operations.
Rationale: Ensuring a responsive multiplayer experience requires a persistent, bi-directional connection.

### II. Strict Server-Side Validation
The server MUST be the absolute source of truth. Every client payload MUST be validated for rate limits, range, and state consistency (e.g., player must be alive to shoot). Server MUST implement idempotency checks using client-provided timestamps to prevent event duplication.
Rationale: Preventing cheating and ensuring a fair game for all participants.

### III. Client-Side Prediction & Reconciliation
To ensure responsiveness, clients SHOULD simulate actions (like spawning a projectile) immediately while waiting for server retransmission.
Rationale: Reducing perceived latency for the local player while maintaining synchronization.

### IV. Deterministic Movement & Vectors
All clients and the server MUST use identical movement logic. Projectile vectors MUST be normalized (magnitude = 1.0 ±0.05) with exactly 2 decimal precision for network payloads to ensure synchronized state across the network.
Rationale: Preventing diverging states that cause visual "jitter" or hit-registration errors.

### V. Automated Validation (Critical Paths)
Critical gameplay mechanics, including WebSocket event handlers and collision math, MUST be covered by unit or integration tests in both Node.js and Unity.
Rationale: Maintaining reliability in a complex real-time environment.

### VI. AI-Assisted Development Traceability
All development sessions utilizing AI agents MUST be documented to ensure transparency and maintainability.
Rationale: Facilitating auditing, debugging, and knowledge transfer in AI-driven projects.

## Technical Constraints
- Unity 2022.3 LTS (2D URP) for the client application.
- Node.js 20 LTS for the backend WebSocket server.
- Vector normalization (x, y) with 2 decimal precision for all network payloads.
- Retransmission latency target: <50ms for optimal gameplay.

## Development Workflow
- Feature specifications MUST be defined in `/specs` before implementation begins.
- All AI-assisted implementation steps MUST be logged in `/specs/[feature]/prompt-logs.md`.
- All WebSocket events MUST follow the documented JSON schema in the spec files.
- Commits SHOULD be atomic and reference the feature or requirement being addressed.

## Governance
This constitution supersedes all other development practices within the Arena Bots project.
Amendments require a version bump (following semantic versioning) and verification that all related specification templates are in sync.
All code reviews and implementation plans MUST verify adherence to these Core Principles.

**Version**: 1.1.0 | **Ratified**: 2026-03-24 | **Last Amended**: 2026-03-24
