# Plan de Implementación - Sistema de Disparo

## Orden de implementación

Paso 1 → Manejador WebSocket del backend
Paso 2 → ShootController en Unity
Paso 3 → ProjectileSpawner en Unity
Paso 4 → Detección de colisiones e impactos

---

## Paso 1 — Manejador WebSocket del backend

Fichero: backend/src/websocket/shootHandler.js

Tareas:
  1.1 Escuchar mensajes de tipo "disparo"
  1.2 Validar que el jugadorId existe en la sesión de partida
  1.3 Validar que el jugador está vivo
  1.4 Validar que la dirección es un vector normalizado (módulo ~1.0)
  1.5 Validar que la posición está dentro de los límites del mapa
  1.6 Aplicar límite de tasa: máx. 10 eventos/seg por jugador
  1.7 Generar un proyectilId único (uuid)
  1.8 Retransmitir disparo_retransmision a todos los clientes de la sesión

---

## Paso 2 — ShootController en Unity

Fichero: unity-client/Assets/Scripts/Shooting/ShootController.cs

Tareas:
  2.1 Detectar la entrada de disparo (Espacio o clic izquierdo)
  2.2 Calcular la dirección de apuntado (normalizar posición ratón - posición jugador)
  2.3 Generar el proyectil localmente (predicción en cliente)
  2.4 Construir el payload JSON del evento de disparo
  2.5 Enviar el payload mediante la conexión WebSocket

---

## Paso 3 — ProjectileSpawner en Unity

Fichero: unity-client/Assets/Scripts/Shooting/ProjectileSpawner.cs

Tareas:
  3.1 Escuchar mensajes WebSocket entrantes de tipo "disparo_retransmision"
  3.2 Extraer proyectilId, posición y dirección del payload
  3.3 Instanciar el prefab del proyectil en la posición recibida
  3.4 Aplicar movimiento en la dirección recibida a velocidad fija (configurable)
  3.5 Destruir el proyectil tras 3 segundos o al colisionar

---

## Paso 4 — Detección de colisiones e impactos

Ficheros:
  unity-client/Assets/Scripts/Shooting/ProjectileCollision.cs
  backend/src/websocket/hitHandler.js

Tareas:
  4.1 Unity: detectar colisión entre el proyectil y el collider del jugador
  4.2 Unity: enviar evento de impacto al servidor con proyectilId y jugadorObjetivoId
  4.3 Backend: validar el evento de impacto (proyectil existe, objetivo vivo)
  4.4 Backend: actualizar la salud y puntuación del jugador
  4.5 Backend: retransmitir impacto_retransmision a todos los clientes
  4.6 Unity: aplicar efecto de impacto en el jugador objetivo
