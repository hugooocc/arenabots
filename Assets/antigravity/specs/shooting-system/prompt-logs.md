# Registro de Prompts — Arena Bots

Este fichero recoge todos los prompts usados durante el desarrollo
guiado por especificaciones (Spec-Driven Development).
Es obligatorio para demostrar el proceso de desarrollo asistido por IA.

---

## Sesión 1 — Manejador WebSocket del backend

Fecha: 24/03/2026
Herramienta IA: Gemini CLI
Spec usada: specs/shooting-system/plan.md (Paso 1)

Prompt:
"Lee specs/shooting-system/foundations.md y specs/shooting-system/plan.md.
Implementa el Paso 1: el manejador WebSocket de disparo en Node.js.
El fichero debe crearse en backend/src/websocket/shootHandler.js.
Usa el formato de payload exacto definido en specs/shooting-system/spec.md."

Resultado:
   - Payload exacto: He usado los campos tipo, jugadorId, partidaId, posicion, direccion y timestamp (del cliente) y tipo: disparo_retransmision, proyectilId (del servidor).
   - Validaciones: Incluye verificación de jugador vivo, límite de tasa (10/seg), normalización de vectores (módulo 1.0 ±0.05) y límites del mapa.
   - Identificadores: Uso de uuid para generar el proyectilId único.
   - Precisión: Redondeo forzado a 2 decimales en el reenvío de posiciones y direcciones.
   - Integración: Actualizado index.js para usar el campo tipo en el enrutamiento de eventos.
   
Problemas encontrados:
- La validación del vector normalizado fallaba inicialmente con precisiones flotantes estrictas; se ajustó el margen a ±0.05.

Prompt de corrección:
"Ajusta la validación del vector de dirección para permitir un margen de error de 0.05 en el módulo para evitar falsos positivos por redondeo en el cliente."

Resultado final:
Implementación robusta con validación de tasa de refresco y límites espaciales.

---

## Sesión 2 — ShootController y Sistema de Proyectiles en Unity

Fecha: 24/03/2026
Herramienta IA: Gemini CLI
Spec usada: specs/shooting-system/plan.md (Pasos 2 y 3)

Prompt:
"Implementa los Pasos 2 y 3 del plan de disparo en Unity. 
Crea ShootController.cs para gestionar el input y envío de eventos al servidor con predicción en cliente.
Crea Projectile.cs para el comportamiento físico básico.
Crea RemoteProjectileSpawner.cs para instanciar disparos de otros jugadores recibidos por WebSocket.
Asegura que el formato JSON coincida con el backend."

Resultado:
- **ShootController.cs**: Implementa predicción en cliente (instancia local inmediata) y envío de `DisparoPayload` al servidor. Incluye lógica de reconciliación inicial mediante timestamps.
- **Projectile.cs**: Lógica de movimiento lineal simple y almacenamiento de metadatos (ID, owner, timestamp).
- **RemoteProjectileSpawner.cs**: Escucha `disparo_retransmision` y genera proyectiles para jugadores remotos, ignorando los propios para evitar duplicados tras la predicción.
- **Formato**: Uso de `Vector2Payload` para asegurar redondeo a 2 decimales en el JSON enviado desde Unity.

Problemas encontrados:
- Los campos de `RetransmissionPayload` en Unity no coincidían exactamente con los nombres de variables del backend (ej: `jugadorId` vs `playerId`). Se normalizó a `jugadorId` siguiendo la spec.

Prompt de corrección:
"Asegúrate de que todos los nombres de campos en los DTOs de Unity coincidan exactamente con la especificación spec.md (jugadorId, partidaId, posicion, direccion)."

Resultado final:
Sistema funcional de disparo multijugador con predicción y sincronización básica.

---

## Sesión 3 — Integración de Matchmaking y WS Rooms

Fecha: 25/03/2026
Herramienta IA: Gemini CLI
Spec usada: specs/matchmaking-system/plan.md (Paso 4)

Prompt:
"Actualiza el sistema de disparo para que solo retransmita eventos a los jugadores en la misma sala (gameId). Modifica shootHandler.js y la conexión inicial en index.js."

Resultado:
- **Broadcasting por sala**: `shootHandler.js` ahora filtra por `client.gameId === partidaId`.
- **WS Context**: La conexión WebSocket ahora extrae `gameId` de la query string para persistirlo en el socket.
- **Unity NetworkManager**: Actualizado para incluir `gameId` en la URL de conexión al entrar en partida.

Problemas encontrados:
- El servidor necesitaba acceso a la instancia `wss` desde el controlador HTTP para el evento `game_started`. Se expuso vía `app.set('wss', wss)`.
