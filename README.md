# Arena Bots - DAMTR3 HUGO CORDOBA

Arena Bots is a fast-paced 2D Top-Down Shooter built with Unity and a Node.js microservices architecture. Survive endless waves of cybernetic enemies in a desert arena, alone or with friends.

## 🚀 Features

- **Microservices Architecture**: Decentralized services for Auth, Game Logic, and Stats.
- **Multiplayer PvE**: Collaborative combat with real-time synchronization via WebSockets.
- **Authoritative Backend**: Node.js services handling hit validation and mob AI.
- **Cyber-Desert Aesthetic**: Custom UI Toolkit interface with glowing neon elements.
- **ML-Agents Integration**: Smart enemies that adapt to player movement.

## 🛠️ Tech Stack

- **Client**: Unity (2D), UI Toolkit, WebSocket.
- **Backend**: Node.js, Express, WebSockets (WS), MongoDB.
- **Infrastructure**: API Gateway (Reverse Proxy), Docker Compose.

## 📦 How to Run

### Backend (Microservices)
1. Go to `Assets/antigravity/backend`.
2. Run the full stack with Docker:
   ```bash
   docker compose up --build
   ```
3. The server will be accessible at `http://localhost:3000` via the Gateway.

### Client
1. Open the project in Unity.
2. Open the `Login` scene in `Assets/antigravity/unity-client/Scenes`.
3. Press **Play**.

## 📂 Documentation
All project documentation (Manual, Diagrams, IA Analysis) is located in `Assets/antigravity/docs/`.

