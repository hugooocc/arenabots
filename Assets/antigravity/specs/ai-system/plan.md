# Plan de Implementación - Agente IA (Un Jugador)

## Orden de implementación

Paso 1 → Integración de Unity ML-Agents
Paso 2 → Diseño del Entorno de Entrenamiento (Arena)
Paso 3 → Implementación del BotAgent (C#)
Paso 4 → Entrenamiento del Modelo y Modo "Un Jugador"

---

## Paso 1 — Integración de ML-Agents

Ficheros: 
- N/A (Configuración del motor)

Tareas:
  1.1 Instalar el paquete de Unity `com.unity.ml-agents` desde el Package Manager.
  1.2 Instalar las dependencias de Python para ML-Agents (`pip install mlagents`) en el entorno local para permitir el entrenamiento.
  1.3 Añadir el componente `Decision Requester` y `Behavior Parameters` al prefab del enemigo.

---

## Paso 2 — Entorno de Entrenamiento

Ficheros: 
- `unity-client/Assets/Scenes/TrainingArena.unity`

Tareas:
  2.1 Crear una escena aislada (sin red ni backend) exclusiva para entrenar al bot.
  2.2 Colocar al Agente (Bot) y a un "Objetivo" (Target) que simule al jugador.
  2.3 El Objetivo debe moverse de forma aleatoria para que el Bot aprenda a perseguirlo y apuntarle.
  2.4 Encapsular el entorno en un prefab padre para instanciarlo múltiples veces (entrenamiento en paralelo).

---

## Paso 3 — Implementación del Script BotAgent

Ficheros: 
- `unity-client/Assets/Scripts/AI/BotAgent.cs`

Tareas:
  3.1 Heredar de `Agent`.
  3.2 Sobrescribir `OnEpisodeBegin()`: resetear posiciones del bot y el jugador objetivo.
  3.3 Sobrescribir `CollectObservations()`: añadir posición del bot, posición del jugador objetivo, vectores de dirección y cooldown de disparo.
  3.4 Sobrescribir `OnActionReceived()`: mapear las acciones (ej: discretas para moverse X/Y, asíncronas para disparar).
  3.5 Asignar recompensas (`SetReward()`, `AddReward()`):
      - Recompensa positiva (+1.0) por acertar un disparo.
      - Recompensa positiva (+2.0) por matar al objetivo.
      - Penalización muy ligera por paso continuo (-0.001) para fomentar la agresividad.
      - Penalización por chocar con paredes (-0.1).

---

## Paso 4 — Modo "Un Jugador" (Integración Final)

Ficheros: 
- `unity-client/Assets/Scripts/GameMode/SinglePlayerManager.cs`

Tareas:
  4.1 Ejecutar el comando `mlagents-learn` en consola y entrenar el modelo hasta obtener un archivo `.onnx`.
  4.2 Importar el modelo `.onnx` a Unity y asignarlo al componente `Behavior Parameters` del Bot.
  4.3 Crear una lógica en Unity donde, si se elige "Jugar contra IA", no se conecta al WebSocket en absoluto.
  4.4 Utilizar instanciación local de proyectiles y colisiones a través de *colliders* de Unity sin la validación del servidor de Node.js.
