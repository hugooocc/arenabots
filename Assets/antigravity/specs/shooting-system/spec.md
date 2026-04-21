# Especificación - Sistema de Disparo

## Actor

Jugador conectado a una partida multijugador.

---

## Disparador

El jugador pulsa el botón de disparo (Espacio o clic izquierdo del ratón).

---

## Payload del Evento WebSocket

Evento enviado del cliente al servidor:

```json
{
  "tipo": "disparo",
  "jugadorId": "string",
  "partidaId": "string",
  "posicion": { "x": 1.23, "y": 4.56 },
  "direccion": { "x": 0.70, "y": 0.71 },
  "timestamp": 1712000000000
}
```

Evento retransmitido del servidor a todos los clientes:

```json
{
  "tipo": "disparo_retransmision",
  "jugadorId": "string",
  "posicion": { "x": 1.23, "y": 4.56 },
  "direccion": { "x": 0.70, "y": 0.71 },
  "proyectilId": "string"
}
```

Nota: la dirección debe ser siempre un vector normalizado (módulo = 1.0).

---

## Comportamiento

1. El jugador pulsa el botón de disparo.
2. El cliente lee la posición actual del jugador y la dirección de apuntado.
3. El cliente genera un proyectil localmente (predicción en cliente).
4. El cliente construye el payload JSON del evento de disparo.
5. El cliente envía el payload al servidor mediante WebSocket.
6. El servidor valida el evento (jugador vivo, límite de tasa, partida existe).
7. El servidor genera un proyectilId único.
8. El servidor retransmite disparo_retransmision a todos los clientes de la sesión.
9. Los clientes remotos generan el proyectil en la posición y dirección recibidas.

---

## Resultado Esperado

Todos los jugadores ven el mismo proyectil aparecer en la posición correcta.
El proyectil se mueve en la dirección normalizada a una velocidad fija.

---

## Casos Límite

- Evento duplicado: el servidor ignora eventos con el mismo timestamp
  del mismo jugador en una ventana de 100ms.
- Dirección inválida: el servidor rechaza eventos cuyo módulo de dirección
  no sea 1.0 (tolerancia ±0.05).
- Posición inválida: el servidor rechaza posiciones fuera de los límites del mapa.
- Jugador muerto: el servidor rechaza eventos de disparo de jugadores muertos.
- Desconexión del cliente: el proyectil continúa en los clientes remotos
  hasta que impacta con algo o sale de la arena.
