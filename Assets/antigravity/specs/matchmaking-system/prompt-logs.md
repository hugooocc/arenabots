# Implementation Prompt Logs: Matchmaking and Lobby System

## Session 2026-03-25: Implementation of Matchmaking System (Backend + Unity Client)

### Objective
Implement a complete matchmaking and lobby system allowing players to create, list, and join game rooms, followed by a seamless transition to the gameplay arena via WebSockets.

### Actions Taken
1.  **Phase 1: Setup**:
    *   Created backend directory structure for `controllers` and `repositories/mongo`.
    *   Created Unity directory structure for `Assets/Scripts/UI`.
    *   Registered `gameRouter` placeholder in `backend/src/index.js`.
2.  **Phase 2: Foundational (Backend)**:
    *   Implemented `Game` Mongoose model with `WAITING`, `PLAYING`, and `FINISHED` statuses.
    *   Implemented `GameRepository.js` for CRUD operations and player management in MongoDB.
3.  **Phase 3 & 4: Game Management & Joining (US1 & US2)**:
    *   Implemented `GameService.js` to handle business logic: creation with owner assignment, available games listing, and joining with capacity/duplicate validation.
    *   Implemented `GameController.js` to expose HTTP endpoints: `POST /api/games`, `GET /api/games`, `POST /api/games/:id/join`, and `POST /api/games/:id/start`.
    *   Integrated endpoints into `backend/src/index.js` with a placeholder JWT authentication middleware.
4.  **Phase 5: Unity Matchmaking UI (US3)**:
    *   Implemented `MainMenuUI.cs` to handle game creation, listing, and joining using `UnityWebRequest`.
    *   Implemented `LobbyUI.cs` to show current players and provide the "Start Game" interface for the host.
5.  **Phase 6: WebSocket Integration & Scene Transition (US4)**:
    *   Updated `backend/src/index.js` to parse `gameId` from WebSocket query parameters and assign it to client objects for room-based broadcasting.
    *   Modified `shootHandler.js` to restrict broadcasting to clients within the same game room.
    *   Updated `GameController.js` to emit a `game_started` event via WebSockets when the host starts the session.
    *   Updated `NetworkManager.cs` in Unity to listen for the `game_started` event and trigger a transition to the "Arena" scene.
6.  **Phase N: Polish**:
    *   Added input validation in `GameController.js` for game names and player limits.

### Constitution Compliance
*   **Principle II (Strict Validation)**: Implemented input validation for room names, player counts, and room capacity.
*   **Principle IV (Architectural Alignment)**: Maintained clean separation between Repository, Service, and Controller layers in the backend.
*   **Principle VI (AI Traceability)**: This log maintains the implementation record for the Matchmaking System.

### Challenges/Retos
*   The `check-prerequisites.ps1` script encountered parameter binding issues, requiring manual directory navigation.
*   Integrating WebSocket room logic required exposing the `wss` instance to Express controllers for event broadcasting.

### Next Steps
*   Implement real-time player list updates in the Unity Lobby using WebSocket events instead of polling.
*   Add logic for host transfer or room cleanup if the host disconnects during the `WAITING` phase.
*   Integrate game-specific settings (map selection, time limits) into the creation flow.
