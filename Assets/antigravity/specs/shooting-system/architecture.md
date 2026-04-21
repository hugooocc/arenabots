# Arquitectura del Sistema

## Descripción General

El sistema sigue una arquitectura cliente-servidor.

Unity actúa como aplicación cliente, mientras que el backend está implementado con Node.js.

---

## Componentes

### Cliente

Aplicación Unity responsable de:

- Renderizar el juego
- Gestionar la entrada del jugador
- Comunicarse con el backend
- Sincronizar los eventos de juego

---

### Backend

Backend en Node.js responsable de:

- Autenticación de usuarios
- Gestión de sesiones de partida
- Comunicación en tiempo real
- Persistencia de datos

---

## Comunicación

### API HTTP

Las peticiones HTTP se usan para:

- Login
- Registro de usuarios
- Crear sesiones de partida
- Listar partidas disponibles
- Guardar resultados de partidas

Unity se comunica con el backend mediante peticiones HTTP usando UnityWebRequest.

---

### WebSockets

Los WebSockets se usan para la comunicación en tiempo real durante el juego.

Los eventos transmitidos incluyen:

- Movimiento del jugador
- Acciones de disparo
- Detección de impactos
- Eventos de inicio y fin de partida

---

## Persistencia de Datos

El sistema usa MongoDB como base de datos.

El backend sigue el patrón Repository para separar la lógica de acceso a datos de la lógica de negocio.

---

## Patrón Repository

El backend implementa las siguientes capas:

Repository:
Gestiona las operaciones de base de datos.

Service:
Implementa la lógica de negocio y validaciones.

Controller:
Gestiona las peticiones y respuestas HTTP.

Estructura de carpetas del backend:

```
backend/
  src/
    repositories/
      mongo/
        UserRepository.js
        GameRepository.js
        ResultRepository.js
      inmemory/
        UserRepository.js
        GameRepository.js
        ResultRepository.js
    services/
      UserService.js
      GameService.js
      ResultService.js
    controllers/
      UserController.js
      GameController.js
      ResultController.js
    websocket/
      shootHandler.js
      hitHandler.js
      moveHandler.js
```

---

## Entidades

### Usuario

Representa a un jugador registrado.

Atributos:

- id
- username
- password_hash
- created_at

---

### Partida

Representa una sesión multijugador.

Atributos:

- id
- players
- maxPlayers
- status
- created_at

---

### Resultado

Representa los resultados de una partida.

Atributos:

- id
- gameId
- winner
- scores
- duration

---

## Stack Técnico

| Capa | Tecnología |
|------|-----------|
| Cliente | Unity 2022.3 LTS (2D URP) |
| Backend | Node.js 20 LTS + Express.js 4.x |
| WebSockets | librería ws (Node.js) + NativeWebSocket (Unity) |
| Base de datos | MongoDB |
| IA | Unity ML-Agents |
| Testing | Repositorios InMemory |
