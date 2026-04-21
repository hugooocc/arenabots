# Especificación - Sistema de Autenticación

## Actor

Jugador no autenticado al iniciar el cliente Unity.

---

## Evento HTTP (Registro de Usuario)

Endpoint: `POST /api/auth/register`
Content-Type: `application/json`

**Body:**
```json
{
  "username": "Player123",
  "password": "mySecurePassword1!"
}
```

**Respuesta Exitosa (HTTP 201 Created):**
```json
{
  "message": "Usuario registrado exitosamente",
  "userId": "60d5ecb8b392d708fc13ae4"
}
```

**Respuestas de Error:**
- HTTP 400 Bad Request (Faltan campos, username inválido/vacio o password muy corto).
- HTTP 409 Conflict (El nombre de usuario ya existe).

---

## Evento HTTP (Login de Usuario)

Endpoint: `POST /api/auth/login`
Content-Type: `application/json`

**Body:**
```json
{
  "username": "Player123",
  "password": "mySecurePassword1!"
}
```

**Respuesta Exitosa (HTTP 200 OK):**
```json
{
  "message": "Login exitoso",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "60d5ecb8b392d708fc13ae4",
    "username": "Player123"
  }
}
```

**Respuestas de Error:**
- HTTP 401 Unauthorized (Usuario no encontrado o contraseña incorrecta).
- HTTP 400 Bad Request (Faltan parámetros).

---

## Comportamiento Post-Comunicación

### Backend:
1. El JWT devuelto DEBE ser proporcionado en las cabeceras de futuras peticiones autenticadas: `Authorization: Bearer <token>`.
2. Para conectar al WebSocket (partida), el cliente Unity deberá pasar este token JWT, posiblemente por Query Params `ws://localhost:3000?token=<token>` o dentro de un payload inicial especial si el wrapper de Unity no soporta Custom Headers.

### Unity Cliente:
1. Si Unity recibe un HTTP 200 del `/api/auth/login`, almacena el token de forma segura para toda la sesión del juego.
2. Inhabilita/Oculta la UI de Login y Muestra la UI del Menú Principal (Lobby/Matchmaking).
