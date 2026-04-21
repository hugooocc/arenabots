# Especificación - Sistema de Matchmaking y Lobby

## Actor

Jugador autenticado (con JWT) buscando jugar.

---

## Evento HTTP (Crear Partida)

Endpoint: `POST /api/games`
Headers: `Authorization: Bearer <token>`
**Body:**
```json
{
  "name": "Sala de Hugo",
  "maxPlayers": 4
}
```

**Respuesta Exitosa (HTTP 201 Created):**
```json
{
  "gameId": "65ab321...",
  "message": "Partida creada",
  "status": "WAITING"
}
```

---

## Evento HTTP (Listar Partidas)

Endpoint: `GET /api/games`
Headers: `Authorization: Bearer <token>`

**Respuesta Exitosa (HTTP 200 OK):**
```json
[
  {
    "id": "65ab321...",
    "name": "Sala de Hugo",
    "currentPlayers": 1,
    "maxPlayers": 4,
    "status": "WAITING"
  }
]
```

---

## Evento HTTP (Unirse a Partida)

Endpoint: `POST /api/games/:id/join`
Headers: `Authorization: Bearer <token>`

**Respuesta Exitosa (HTTP 200 OK):**
```json
{
  "message": "Te has unido a la partida",
  "game": {
    "id": "65ab321...",
    "players": ["60d5ec...", "71e6fd..."]
  }
}
```

---

## Reglas de Negocio (Backend)

1. **Límites:** Un jugador no puede estar en dos partidas simultáneamente. Si se une a una partida, debe abandonar cualquier otra sesión "WAITING" en la que estuviese.
2. **Llenado:** Si `currentPlayers == maxPlayers`, la partida rechaza nuevos `POST /join` (HTTP 403 Forbidden - Sala llena).
3. **Inicio:** Cuando el anfitrión inicia la partida, el estado cambia a `PLAYING` y deja de aparecer en el `GET /api/games`.

---

## Comportamiento del WebSocket post-Matchmaking

Una vez que Unity entra en el estado de partida `PLAYING`:
1. Unity establece conexión: `new WebSocket("ws://localhost:3000?gameId=65ab321...&token=...")`.
2. Servidor verifica el token y que el usuario pertenece al `gameId`.
3. Servidor emite un evento WS de tipo `player_joined` a los demás clientes en ese `gameId`.
