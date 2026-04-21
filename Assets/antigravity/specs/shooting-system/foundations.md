# Fundamentos - Sistema de Disparo

## Contexto

Arena Bots es un videojuego 2D multijugador desarrollado con Unity (cliente)
y Node.js (servidor).

Los jugadores se conectan a sesiones de partida compartidas e interactúan
en tiempo real. Disparar proyectiles a otros jugadores es una mecánica central.

---

## Objetivo

Implementar un sistema de disparo multijugador donde:

- Los jugadores puedan disparar proyectiles en cualquier dirección
- Los eventos de disparo se envíen al servidor de forma inmediata
- El servidor valide y retransmita los eventos a todos los clientes
- Todos los clientes generen y muevan el proyectil de forma idéntica

---

## Stack Técnico

Cliente:
  Unity 2022.3 LTS (2D URP)
  NativeWebSocket (librería WebSocket para Unity)

Backend:
  Node.js 20 LTS
  Express.js 4.x
  ws (librería WebSocket para Node.js)

---

## Restricciones

- WebSocket debe usarse para todos los eventos de juego en tiempo real
- HTTP no debe usarse para eventos de disparo (demasiado lento)
- La dirección del proyectil debe ser un vector normalizado (x, y)
- Máximo 10 eventos de disparo por segundo por jugador (límite de tasa)
- El servidor debe retransmitir en menos de 50ms tras recibir el evento
- El cliente Unity debe generar el proyectil localmente de forma inmediata
  (predicción en cliente) y confirmarlo tras la retransmisión del servidor
- Todas las posiciones usan float con 2 decimales de precisión
