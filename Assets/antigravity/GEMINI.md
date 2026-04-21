# antigravity Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-24

## Active Technologies

- C# (Unity 2022.3 LTS), JavaScript (Node.js 20 LTS) + NativeWebSocket (Unity), ws (Node.js), Express.js 4.x (Node.js) (001-multiplayer-shooting)

## Project Structure

```text
src/
tests/
```

## Commands

npm test; npm run lint

## Code Style

C# (Unity 2022.3 LTS), JavaScript (Node.js 20 LTS): Follow standard conventions

## Recent Changes

- 001-multiplayer-shooting: Added C# (Unity 2022.3 LTS), JavaScript (Node.js 20 LTS) + NativeWebSocket (Unity), ws (Node.js), Express.js 4.x (Node.js)

<!-- MANUAL ADDITIONS START -->
## Recent Architectural Changes & Fixes (Apr 2026)

### Frontend (Unity)
- **UI System Migration**: Transitioned from legacy Canvas (uGUI) to **UI Toolkit**. Screens like Login and Main Menu now use modern web-like structure (`.uxml`) and styling (`.uss`) for improved scalability, styling, and codebase cleanliness.
- **Input Management**: Migrated away from legacy `UnityEngine.Input` to the new `UnityEngine.InputSystem`, resolving event and input crashing.
- **Network Handshake**: Added robust null checks for the singleton WebSocket connection to permit local "Single Player" mode testing without network crashes.

### Backend (Node.js & MongoDB)
- **Containerization**: Implemented Docker orchestration for the entire backend stack. A `docker-compose.yml` file now manages both the Node.js backend container and the persistent MongoDB database.
- **Authentication**: Patched the generated Auth system to properly securely hash user passwords with `bcrypt` before storing them in MongoDB.
- **Database Connection**: Configured and initialized the `mongoose` connection URI injection, preventing Node request timeouts.
<!-- MANUAL ADDITIONS END -->
