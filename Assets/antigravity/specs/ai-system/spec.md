# Especificación - Agente IA (ML-Agents)

## Entradas del Modelo (Observaciones)

Espacio de observación (Continuous):
- `Vector2` Posición local del Bot (2 floats).
- `Vector2` Posición local del Jugador (2 floats).
- `Vector2` Velocidad actual del Bot (2 floats).
- `float` Cooldown de disparo restante del Bot (1 float).
- Distancia al jugador (1 float).
- Raycasts (Opcional): Si hay obstáculos en la arena, usar `RayPerceptionSensor2D` para detectar paredes y proyectiles enemigos.

Total de floats continuos: ~8 a 20 (dependiendo de raycasts).

---

## Salidas del Modelo (Acciones)

Modo: Discreto (Discrete Branches).

- **Rama 0 (Movimiento Horizontal):** 0 = Nada, 1 = Izquierda, 2 = Derecha.
- **Rama 1 (Movimiento Vertical):** 0 = Nada, 1 = Abajo, 2 = Arriba.
- **Rama 2 (Disparo):** 0 = No Disparar, 1 = Disparar hacia el jugador. (Nota: Para facilitar el entrenamiento, el agente siempre ajusta el cañón hacia el jugador de forma procedural, la IA sólo decide "cuándo" apretar el gatillo).

---

## Configuración de Entrenamiento (YAML)

Fichero sugerido `trainer_config.yaml`:
```yaml
behaviors:
  ArenaBot:
    trainer_type: ppo
    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.99
      num_epoch: 3
    network_settings:
      normalize: false
      hidden_units: 128
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 2000000
    time_horizon: 64
    summary_freq: 50000
```

---

## Comportamiento del Entorno Single Player

1.  **Aislamiento:** Al iniciar "Un jugador", el cliente NO envía ninguna petición a `http://localhost:3000/api` y NO arranca la conexión WebSocket.
2.  **Autoridad Local:** Todo el daño, validación de estado y límite de disparos ocurre procesado autoritativamente en la propia memoria del cliente Unity, reutilizando los scripts como `ShootController` pero bypasseando o falseando las llamadas a red.
3.  **Ciclo:** Al matar a la IA, la IA se resetea en un nuevo punto aleatorio incrementando el contador de puntos del jugador. Si la IA mata al jugador, termina la partida.
