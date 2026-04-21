# Tasks: Multiplayer Shooting System

**Input**: Design documents from `/specs/001-multiplayer-shooting/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: This project uses a TDD-inspired approach for critical gameplay paths. Tests are included for the server-side validation and client-side projectile logic as per Constitution Principle V.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Parallelizable task.
- **[Story]**: [US1] Real-time Shooting, [US2] Multiplayer Synchronization, [US3] Server Validation.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure.

- [X] T001 [P] Create directory structure for `unity-client/Assets/Scripts/Shooting/`
- [X] T002 [P] Create directory structure for `backend/src/websocket/`
- [X] T003 [P] Configure Node.js project with `ws` and `express` dependencies
- [X] T004 [P] Initialize Unity Project with `NativeWebSocket` package

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure for WebSocket communication.

- [X] T005 Implement base WebSocket server in `backend/src/index.js`
- [X] T006 Implement basic WebSocket client connector in `unity-client/Assets/Scripts/Shooting/NetworkManager.cs`
- [X] T007 [P] Create `Projectile` and `PlayerSession` models in `backend/src/models/`
- [X] T008 [P] Setup basic error handling and logging for WebSocket events in backend

---

## Phase 3: User Story 1 - Real-time Shooting (Priority: P1) 🎯 MVP

**Goal**: Implement local firing with client-side prediction.

**Independent Test**: Press fire button; projectile spawns and moves instantly in Unity without server connection.

### Implementation for User Story 1

- [X] T009 [P] [US1] Create `Projectile.cs` in `unity-client/Assets/Scripts/Shooting/` for movement logic
- [X] T010 [US1] Implement `ShootController.cs` in Unity to handle local input and projectile spawning
- [X] T011 [US1] Implement `disparo` event generation with 2-decimal precision in Unity
- [X] T012 [US1] Update `NetworkManager.cs` to send `disparo` payload to server

**Checkpoint**: US1 complete - Local shooting is functional.

---

## Phase 4: User Story 3 - Server Validation (Priority: P3)

**Goal**: Implement server-side validation for cheating prevention (rate limiting, state, idempotency).

**Independent Test**: Use a test script to send invalid events (high rate, dead player) and verify rejection.

### Tests for User Story 3

- [X] T013 [P] [US3] Unit test for rate limiting logic in `backend/tests/validation.test.js`
- [X] T014 [P] [US3] Unit test for vector normalization validation in `backend/tests/validation.test.js`
- [X] T015 [P] [US3] Unit test for idempotency (timestamp) check in `backend/tests/validation.test.js`

### Implementation for User Story 3

- [X] T016 [US3] Implement `shootHandler.js` in `backend/src/websocket/` to receive `disparo` events
- [X] T017 [US3] Implement player state validation (isAlive) in `shootHandler.js`
- [X] T018 [US3] Implement rate limiting (10 shots/sec) logic in `shootHandler.js`
- [X] T019 [US3] Implement vector normalization check (magnitude 1.0 ±0.05)
- [X] T020 [US3] Implement idempotency check using client timestamps

**Checkpoint**: US3 complete - Server effectively validates and filters firing events.

---

## Phase 5: User Story 2 - Multiplayer Synchronization (Priority: P2)

**Goal**: Retransmit validated events and spawn remote projectiles.

**Independent Test**: Fire from Client A; see projectile move on Client B.

### Implementation for User Story 2

- [X] T021 [US2] Update `shootHandler.js` to generate `proyectilId` and retransmit `disparo_retransmision`
- [X] T022 [US2] Implement `RemoteProjectileSpawner.cs` in Unity to handle `disparo_retransmision` events
- [X] T023 [US2] Add logic to Unity `ShootController` to reconcile local projectiles with server retransmissions (matching IDs/timestamps)

**Checkpoint**: US2 complete - Shooting is synchronized across the network.

---

## Phase N: Polish & Cross-Cutting Concerns

- [X] T024 [P] Update `specs/001-multiplayer-shooting/prompt-logs.md` with implementation session details (Principle VI)
- [X] T025 [P] Documentation updates in `quickstart.md`
- [X] T026 Code cleanup and refactoring in `shootHandler.js` and `ShootController.cs`
- [X] T027 Run full synchronization validation per `quickstart.md`

---

## Dependencies & Execution Order

1. **Setup (Phase 1)** → **Foundational (Phase 2)**
2. **Foundational (Phase 2)** BLOCKS all user stories.
3. **User Story 1 (P1)** can proceed after Phase 2.
4. **User Story 3 (P3)** implements the backend validation logic needed for retransmission.
5. **User Story 2 (P2)** completes the loop by integrating backend retransmission with client-side synchronization.

## Parallel Opportunities

- Setup tasks (T001-T004) can run in parallel.
- Unity models (T009) and Backend models (T007) can run in parallel.
- All US3 tests (T013-T015) can run in parallel before implementation.
