# Tasks: AI System (ML-Agents)

**Input**: Design documents from `/specs/ai-system/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md (if available)

**Tests**: Tests are included for the AI logic (observations and actions) to ensure the model can be trained effectively.

**Organization**: Tasks are grouped by user story (ML-Agents Integration, Training Arena, BotAgent Implementation, Single Player Mode) to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Parallelizable task.
- **[Story]**: [US1] ML-Agents Integration, [US2] Training Environment, [US3] Bot Intelligence, [US4] Single Player Mode.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and ML-Agents dependency setup.

- [X] T001 Install Unity package `com.unity.ml-agents` via Package Manager
- [X] T002 Install Python dependencies: `pip install mlagents` in local environment
- [X] T003 [P] Create directory structure for `unity-client/Assets/Scripts/AI/`
- [X] T004 [P] Create directory structure for `unity-client/Assets/ML-Models/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Basic agent components and configuration.

- [X] T005 Create base `BotAgent.cs` in `unity-client/Assets/Scripts/AI/` inheriting from `Agent`
- [X] T006 Create `trainer_config.yaml` in project root with PPO settings from spec.md
- [X] T007 [P] Create a basic "Target" script for the training dummy in `unity-client/Assets/Scripts/AI/TrainingTarget.cs`

---

## Phase 3: User Story 1 - Training Environment (Priority: P1)

**Goal**: Create a functional arena for the bot to learn.

**Independent Test**: Open `TrainingArena` scene; Target moves randomly, and Bot is present.

### Implementation for User Story 1

- [ ] T008 [US1] Create `TrainingArena.unity` scene in `unity-client/Assets/Scenes/`
- [X] T009 [US1] Implement random movement logic in `TrainingTarget.cs` for training
- [ ] T010 [US1] Setup the Agent prefab with `Decision Requester` and `Behavior Parameters`
- [ ] T011 [US1] Create an "Environment" prefab containing the Bot, Target, and Arena bounds

**Checkpoint**: US1 complete - Training environment is ready for the agent logic.

---

## Phase 4: User Story 2 - Bot Intelligence (Priority: P1) 🎯 MVP

**Goal**: Implement the AI logic for observations, actions, and rewards.

**Independent Test**: Run `mlagents-learn`; the bot starts receiving rewards and its mean reward increases over time.

### Implementation for User Story 2

- [X] T012 [US2] Implement `OnEpisodeBegin()` in `BotAgent.cs` to reset positions
- [X] T013 [US2] Implement `CollectObservations()` in `BotAgent.cs` (8-20 floats as per spec)
- [X] T014 [US2] Implement `OnActionReceived()` in `BotAgent.cs` for movement and shooting
- [X] T015 [US2] Implement reward logic (Hit: +1.0, Kill: +2.0, Step: -0.001, Wall: -0.1)
- [X] T016 [US2] Add `Heuristic()` method to `BotAgent.cs` for manual testing with keyboard

**Checkpoint**: US2 complete - Bot is capable of learning and being controlled manually for testing.

---

## Phase 5: User Story 3 - Model Training & Integration (Priority: P2)

**Goal**: Train the model and use the brain in the agent.

**Independent Test**: Assign `.onnx` model to Bot; Bot pursues and shoots at Target autonomously.

### Implementation for User Story 3

- [ ] T017 [US3] Execute `mlagents-learn` and train until convergence (~2M steps)
- [ ] T018 [US3] Export and import `ArenaBot.onnx` into `unity-client/Assets/ML-Models/`
- [ ] T019 [US3] Update Bot prefab to use the trained `.onnx` model in `Behavior Parameters`

**Checkpoint**: US3 complete - Bot is now "intelligent" using a trained neural network.

---

## Phase 6: User Story 4 - Single Player Mode (Priority: P3)

**Goal**: Integrate the AI into a non-networked game mode.

**Independent Test**: Select "Single Player"; game starts without WebSocket connection, and AI enemies spawn.

### Implementation for User Story 4

- [X] T020 [US4] Create `SinglePlayerManager.cs` in `unity-client/Assets/Scripts/GameMode/`
- [X] T021 [US4] Implement "Local Authority" logic to bypass WebSocket calls in `ShootController.cs`
- [X] T022 [US4] Implement collision and damage logic for local-only projectiles
- [X] T023 [US4] Implement respawn loop: killing AI resets its position and increments score

**Checkpoint**: US4 complete - Fully functional single player mode against AI.

---

## Phase N: Polish & Cross-Cutting Concerns

- [X] T024 [P] Update `specs/ai-system/prompt-logs.md` with implementation details
- [ ] T025 [P] Performance optimization: Ensure Raycasts don't impact FPS
- [X] T026 Code cleanup: Refactor `BotAgent.cs` for readability
- [ ] T027 Final validation: Run a full "Single Player" match from start to finish

---

## Dependencies & Execution Order

1. **Setup (Phase 1)** → **Foundational (Phase 2)**
2. **Phase 2** BLOCKS all AI logic.
3. **User Story 1 (Arena)** and **User Story 2 (Logic)** can be worked on in parallel once Foundational is done.
4. **User Story 3 (Training)** depends on US1 and US2 being complete.
5. **User Story 4 (Single Player)** integrates the trained model into the game.

## Parallel Opportunities

- Setup tasks (T001-T004) can run in parallel.
- Environment setup (US1) and Agent coding (US2) can run in parallel.
- Documentation (T024) and Optimization (T025) can run in parallel.

---

## Implementation Strategy

### MVP First (User Story 1 & 2)

1. Setup the environment and the script.
2. Verify the bot can be controlled via `Heuristic` (keyboard).
3. Verify `mlagents-learn` can start a session.

### Incremental Delivery

1. **Arena Ready**: Scene and Target are functional.
2. **Brain Ready**: Agent logic is coded and reward-balanced.
3. **Trained Bot**: Model is integrated and works in-scene.
4. **Game Integration**: Single player mode is available in the UI.
