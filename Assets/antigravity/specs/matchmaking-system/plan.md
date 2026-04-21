# Plan de Implementación - Sistema de Matchmaking y Lobby

## Orden de implementación

Paso 1 → Modelos y Repositorios de Partida (Game)
Paso 2 → Endpoints HTTP para Gestión de Partidas (Crear / Listar / Unirse)
Paso 3 → Interfaz de Unity (Menú Principal y Lobby)
Paso 4 → Conexión WebSocket al unirse e inicio de la partida

---

## Paso 1 — Modelos y Repositorios

Ficheros: 
- `backend/src/models/Game.js`
- `backend/src/repositories/mongo/GameRepository.js`

Tareas:
  1.1 Crear el modelo `Game` en Mongoose: `name` (string), `maxPlayers` (number), `players` (array de ObjectIds de User), `status` (enum: 'WAITING', 'PLAYING', 'FINISHED').
  1.2 Implementar métodos en `GameRepository.js`: `createGame()`, `findAvailableGames()`, `addPlayerToGame()`, `updateGameStatus()`, `findGameById()`.

---

## Paso 2 — Endpoints HTTP

Ficheros: 
- `backend/src/controllers/GameController.js`
- `backend/src/services/GameService.js`

Tareas:
  2.1 Endpoint `POST /api/games` para crear una nueva partida (requiere auth token).
  2.2 Endpoint `GET /api/games` para listar partidas en estado 'WAITING'.
  2.3 Endpoint `POST /api/games/:id/join` para unirse a una partida. Validar que la partida no esté llena y que el jugador no esté ya dentro.
  2.4 Endpoint `POST /api/games/:id/start` (solo el creador o si la sala está llena) para cambiar el estado a 'PLAYING'.

---

## Paso 3 — UI en Unity

Ficheros: 
- `unity-client/Assets/Scripts/UI/MainMenuUI.cs`
- `unity-client/Assets/Scripts/UI/LobbyUI.cs`

Tareas:
  3.1 Crear panel "Crear Partida" (Input para nombre, Slider para max players).
  3.2 Crear panel "Listar Partidas" (ScrollView poblado con prefabs de partidas devueltos por el GET).
  3.3 Botón de "Unirse" en cada prefab que llame al POST `/api/games/:id/join`.
  3.4 Panel de "Lobby" que muestra los jugadores unidos actuales (usando polling o un evento WebSocket inicial).

---

## Paso 4 — Transición a la Arena (WebSocket)

Ficheros: 
- `backend/src/websocket/index.js`
- `unity-client/Assets/Scripts/NetworkManager.cs`

Tareas:
  4.1 Al pulsar "Iniciar Partida" o cuando el servidor emita el evento `game_started` por WS, cargar la escena de la Arena en Unity.
  4.2 El cliente Unity se conecta al WebSocket usando `ws://localhost:3000?gameId=XXX&token=YYY`.
  4.3 El Backend registra la conexión WS vinculándola al `gameId` en un diccionario de salas (Rooms).
