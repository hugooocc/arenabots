# Tasks: Sistema de Matchmaking y Lobby

**Input**: Design documents from `/specs/matchmaking-system/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: [US1] Create/List Games, [US2] Join/Start Games, [US3] Unity UI, [US4] WS Transition.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic directory structure

- [X] T001 [P] Create directory structure for backend controllers and repositories at `backend/src/controllers/` and `backend/src/repositories/mongo/`
- [X] T002 [P] Create directory structure for Unity UI scripts at `unity-client/Assets/Scripts/UI/`
- [X] T003 Register `gameRouter` placeholder in `backend/src/index.js` to enable routing development

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data models and basic infrastructure

- [X] T004 Create `Game` Mongoose schema with `name`, `maxPlayers`, `players` (ObjectIds), and `status` (WAITING, PLAYING, FINISHED) in `backend/src/models/Game.js`
- [X] T005 [P] Implement `GameRepository.createGame()` and `GameRepository.findGameById()` in `backend/src/repositories/mongo/GameRepository.js`
- [X] T006 [P] Implement `GameRepository.findAvailableGames()` and `GameRepository.updateGameStatus()` in `backend/src/repositories/mongo/GameRepository.js`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Create and List Games (Priority: P1) 🎯 MVP

**Goal**: Allow players to create game rooms and browse available sessions.

**Independent Test**: Send `POST /api/games` with name; verify 201 response. Then `GET /api/games`; verify game appears in list.

### Implementation for User Story 1

- [X] T007 [US1] Implement `GameService.createGame()` with owner assignment in `backend/src/services/GameService.js`
- [X] T008 [US1] Implement `GameService.getAvailableGames()` in `backend/src/services/GameService.js`
- [X] T009 [US1] Implement `create` and `list` methods in `backend/src/controllers/GameController.js`
- [X] T010 [US1] Define `POST /api/games` and `GET /api/games` routes in `backend/src/index.js`

**Checkpoint**: US1 complete - Players can create and list games via API.

---

## Phase 4: User Story 2 - Join and Start Games (Priority: P1)

**Goal**: Enable players to join rooms and transition the game to playing status.

**Independent Test**: Join game with second user via `POST /join`; verify player array updates. Call `/start`; verify status becomes 'PLAYING'.

### Implementation for User Story 2

- [X] T011 [US2] Implement `GameRepository.addPlayerToGame()` in `backend/src/repositories/mongo/GameRepository.js`
- [X] T012 [US2] Implement `GameService.joinGame()` with capacity check (maxPlayers) and duplicate entry prevention in `backend/src/services/GameService.js`
- [X] T013 [US2] Implement `GameService.startGame()` (validating status is 'WAITING') in `backend/src/services/GameService.js`
- [X] T014 [US2] Implement `join` and `start` endpoints in `backend/src/controllers/GameController.js`
- [X] T015 [US2] Define `POST /api/games/:id/join` and `POST /api/games/:id/start` routes in `backend/src/index.js`

**Checkpoint**: US2 complete - Full matchmaking flow (Join/Start) functional on backend.

---

## Phase 5: User Story 3 - Unity Matchmaking UI (Priority: P2)

**Goal**: Provide a graphical interface for game creation and browsing in Unity.

**Independent Test**: Use Unity UI to create a room; verify it exists in DB. Join room; verify UI updates with player list.

### Implementation for User Story 3

- [X] T016 [P] [US3] Create `MainMenuUI.cs` script with `CreateGame()` and `RefreshGamesList()` methods in `unity-client/Assets/Scripts/UI/MainMenuUI.cs`
- [X] T017 [P] [US3] Create `LobbyUI.cs` script to display list of joined players in `unity-client/Assets/Scripts/UI/LobbyUI.cs`
- [X] T018 [US3] Integrate Unity HTTP requests using `UnityWebRequest` to call backend endpoints in `unity-client/Assets/Scripts/UI/MainMenuUI.cs`
- [X] T019 [US3] Implement dynamic list population using a GameItem prefab in `unity-client/Assets/Scripts/UI/MainMenuUI.cs`

**Checkpoint**: US3 complete - Matchmaking fully playable from Unity client.

---

## Phase 6: User Story 4 - WebSocket Integration & Scene Transition (Priority: P3)

**Goal**: Seamlessly transition from the lobby to the gameplay arena.

**Independent Test**: Start game from lobby; verify client connects to WS and loads 'Arena' scene.

### Implementation for User Story 4

- [X] T020 [US4] Configure `backend/src/websocket/index.js` to handle `gameId` subscription for room-based events
- [X] T021 [US4] Implement `player_joined` and `game_started` broadcast logic in `backend/src/websocket/index.js`
- [X] T022 [US4] Update `NetworkManager.cs` to handle scene loading upon receiving `game_started` event in `unity-client/Assets/Scripts/NetworkManager.cs`

**Checkpoint**: US4 complete - End-to-end flow from Menu to Gameplay established.

---

## Phase N: Polish & Cross-Cutting Concerns

- [X] T023 [P] Add input validation for game names and player counts in `backend/src/controllers/GameController.js`
- [X] T024 [P] Implement automatic room cleanup on server shutdown in `backend/src/services/GameService.js`
- [X] T025 Final integration test of the complete player journey: Login -> Create -> Join -> Start -> Arena

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories.
- **User Stories (Phase 3+)**: All depend on Phase 2.
  - US1 and US2 are priority P1 (Core Backend).
  - US3 (Unity) depends on US1/US2 backend endpoints.
  - US4 (WS) depends on US2 (Start Game) logic.

### Parallel Opportunities

- Unity UI layout (T016, T017) can start as soon as Phase 1 is done.
- Backend repositories and models (T004-T006) can be developed in parallel.
- Polish tasks (T023, T024) can run in parallel with US4.

---

## Implementation Strategy

1. **MVP**: Complete Phase 1 through Phase 4 to have a functional Backend API.
2. **Unity Integration**: Complete Phase 5 to connect the UI.
3. **Gameplay Bridge**: Complete Phase 6 to link matchmaking with the arena.
