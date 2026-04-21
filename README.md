# Arena Bots - Multiplayer PvE Horde Survival

Arena Bots is a fast-paced 2D Top-Down Shooter built with Unity and Node.js. Survive endless waves of cybernetic enemies in a desert arena, alone or with friends.

## 🚀 Features

- **Multiplayer PvE**: Collaborative combat with real-time synchronization.
- **Authoritative Backend**: Node.js server handling hit validation and mob AI.
- **Cyber-Desert Aesthetic**: Custom UI Toolkit interface with glowing neon elements.
- **Stats System**: Authoritative tracking of kills and survival time.
- **Smart Pause**: Single-player physical pause and multiplayer menu system.

## 🛠️ Tech Stack

- **Client**: Unity (2D), UI Toolkit, WebSocket.
- **Backend**: Node.js, Express, Socket.io (WS), MongoDB (Auth).
- **Style**: Vanilla CSS (USS) for premium cyber aesthetics.

## 📦 How to Run

### Backend
1. Go to `Assets/antigravity/backend`
2. Run `docker compose up --build` or `npm install && npm start`

### Client
1. Open the project in Unity.
2. Ensure you are on the `Login` or `MainMenu` scene.
3. Press **Play**.

## 📄 License
ISC
