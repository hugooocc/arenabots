# Documento de Diseño del Juego

## Nombre del Proyecto
Arena Bots: Hordes

## Curso
2DAM 2025-2026

## Descripción General
Arena Bots es un videojuego 2D multijugador cooperativo ("PvE") desarrollado con Unity. Los jugadores se unen a una misma sala (Arena) donde deben colaborar para sobrevivir el mayor tiempo posible a oleadas infinitas de bots enemigos (estilo "Survivor"). Cuanto más tiempo pase, más y peores bots aparecerán.

---

## Jugabilidad

### Mecánicas Principales
- **Movimiento 360º**: Por una arena gigante y cerrada.
- **Disparo (PvE)**: Los disparos se dirigen a los bots/mobs controlados por la IA del servidor.
- **Oleadas y Dificultad Dinámica**: El servidor incrementa el "ruido/tiempo" y genera más enemigos continuamente.
- **Puntuación**: Dictada por el tiempo de supervivencia grupal y la cantidad de mobs aniquilados.

### Cambios Arquitectónicos Recientes (Implementados)
Para soportar este modo y mejorar la calidad del código, el proyecto ha migrado sus cimientos tecnológicos:

#### Frontend (Unity)
1. **Migración a UI Toolkit**: Todas las pantallas de menús han dejado de usar `Canvas/uGUI`. Ahora se usa UXML y USS (similar a HTML/CSS) para interfaces más responsivas, profesionales con esquemas de colores futuristas.
2. **Nuevo Input System**: Para evitar cruce de eventos de teclado.
3. **Flujo de Escenas**: `Login -> MainMenu -> Arena`. La sala elegida se guarda en un objeto persistente (`GameSession.CurrentGameId`), de forma que el `NetworkManager` se auto-conecta al WebSocket del modo multijugador en cuanto el mapa carga.

#### Backend (Node.js & MongoDB)
1. **Autenticación Fuerte**: Integración de encriptación Bcrypt en la creación de cuentas.
2. **Dockerización Profesional**: Todo el entorno se levanta con `docker-compose up --build`. Levanta de forma automática el contenedor de Node.js en el puerto 3000 y el de MongoDB en el 27017 conectándolos mediante una red virtual interna.

---

## Pantallas

### Login / Registro (UI Toolkit)
Interacción inicial del jugador. Uso de visuales neón. Validado contra MongoDB mediante API REST.

### Menú Principal (UI Toolkit)
- Lista en tiempo real de "Salas de Supervivencia" conectándose al endpoint `/api/games`.
- Opción de lanzar una sala nueva estableciendo la capacidad cooperativa.

### Arena de Juego
Mapa cerrado de grandes dimensiones con coberturas. Un `WaveManager` debe inyectar zombis-bots desde los límites.

---

## Tecnologías Principales Revisadas

- **Servidor y Lógica**: Node.js, Express, WebSockets (ws), Mongoose.
- **Despliegue Local**: Docker y Docker Compose
- **Gráficos y UI**: Unity 2D, UIElements (UI Toolkit)
- **Base de Datos**: MongoDB (Docker Component)
