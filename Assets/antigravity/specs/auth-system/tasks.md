# Tasks: Authentication System

**Input**: Design documents from `/specs/auth-system/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: [US1] User Registration, [US2] User Login, [US3] Unity Client Integration.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Install dependencies: `mongoose`, `bcrypt`, `jsonwebtoken` in `backend/`
- [X] T002 [P] Create directory structure for `backend/src/repositories/mongo/`, `backend/src/services/`, and `backend/src/controllers/`
- [X] T003 [P] Create directory structure for `unity-client/Assets/Scripts/Auth/` and `unity-client/Assets/Scripts/UI/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure for data persistence and models

- [X] T004 Create `User` model with Mongoose in `backend/src/models/User.js`
- [X] T005 Implement `UserRepository` with `createUser` and `findUserByUsername` in `backend/src/repositories/mongo/UserRepository.js`

**Checkpoint**: Foundation ready - Authentication logic implementation can now begin.

---

## Phase 3: User Story 1 - User Registration (Priority: P1) 🎯 MVP

**Goal**: Allow new players to create accounts with secure password storage.

**Independent Test**: Send `POST /api/auth/register` via Postman/Curl; verify user is created in MongoDB with a hashed password.

### Implementation for User Story 1

- [X] T006 [US1] Implement `UserService.register` with bcrypt hashing in `backend/src/services/UserService.js`
- [X] T007 [US1] Create `UserController.js` and implement `register` endpoint in `backend/src/controllers/UserController.js`
- [X] T008 [US1] Register auth routes in `backend/src/index.js`

**Checkpoint**: User Registration is fully functional.

---

## Phase 4: User Story 2 - User Login (Priority: P2)

**Goal**: Authenticate existing players and issue JWT tokens for session management.

**Independent Test**: Send `POST /api/auth/login` with valid credentials; verify receipt of a signed JWT.

### Implementation for User Story 2

- [X] T009 [US2] Implement `UserService.login` with password verification and JWT signing in `backend/src/services/UserService.js`
- [X] T010 [US2] Implement `login` endpoint in `backend/src/controllers/UserController.js`
- [X] T011 [US2] Add JWT secret configuration to `.env` and environment management

**Checkpoint**: User Login and JWT issuance are fully functional.

---

## Phase 5: User Story 3 - Unity Client Integration (Priority: P3)

**Goal**: Provide in-game UI for registration and login, and manage the authentication token.

**Independent Test**: Use the Login UI in Unity; verify successful login transitions to the main menu and stores the JWT.

### Implementation for User Story 3

- [X] T012 [US3] Implement `AuthManager.cs` in `unity-client/Assets/Scripts/Auth/AuthManager.cs` for HTTP requests
- [X] T013 [US3] Create `LoginUI.cs` in `unity-client/Assets/Scripts/UI/LoginUI.cs` for handling user input
- [X] T014 [US3] Create `GameSession.cs` to persist the JWT token during the game session

**Checkpoint**: Unity client can now authenticate against the backend.

---

## Phase N: Polish & Cross-Cutting Concerns

- [X] T015 [P] Update `specs/auth-system/prompt-logs.md` with session details
- [X] T016 [P] Add input validation middleware for registration and login in `backend/src/middleware/validation.js`
- [X] T017 Code cleanup and refactoring in `UserService.js`

---

## Dependencies & Execution Order

1. **Phase 1 (Setup)** → **Phase 2 (Foundational)**
2. **Phase 2** BLOCKS Phase 3 and 4.
3. **Phase 3 (US1)** should be completed before **Phase 4 (US2)** to have users to log in with.
4. **Phase 5 (Unity)** depends on Phase 3 and 4 being ready in the backend.

## Parallel Opportunities

- Setup tasks (T002, T003) can run in parallel.
- Unity UI development (T013) can start in parallel with backend development.
- Documentation and Polish tasks (T015, T016) can run in parallel at the end.
