# 📊 Diagrames del Sistema - Arena Bots

Aquests diagrames detallen el funcionament intern i l'arquitectura del projecte.

## 1. Casos d'Ús
Representa les interaccions principals de l'usuari amb el sistema.

```mermaid
useCaseDiagram
    actor Usuari
    Usuari --> (Registrar-se / Login)
    Usuari --> (Crear / Unir-se a Partida)
    Usuari --> (Jugar Partida PvE)
    Usuari --> (Sincronitzar Moviment i Trets)
    Usuari --> (Veure Estadístiques)
```

## 2. Seqüència: Procés de Joc (WebSocket)
Mostra com flueix la informació en temps real durant una partida multijugador.

```mermaid
sequenceDiagram
    participant Unity as Client Unity
    participant Gateway as Gateway (Port 3000)
    participant GameSrv as Game Service (Port 3002)

    Unity->>Gateway: Upgrade Connection (WebSocket)
    Gateway->>GameSrv: Proxy Connection
    GameSrv-->>Unity: Connection Established
    
    loop Durante la Partida
        Unity->>GameSrv: enviar (tipo: 'movimiento')
        GameSrv-->>Unity: broadcast (tipo: 'jugador_movido')
        Unity->>GameSrv: enviar (tipo: 'disparo')
        GameSrv-->>Unity: broadcast (tipo: 'enemic_impactat')
    end

    Unity->>GameSrv: Close Connection
    GameSrv->>GameSrv: finishGame() stats update
```

## 3. Entitat-Relació (Model de Dades)
Estructura de dades emmagatzemada a MongoDB.

```mermaid
erDiagram
    USER ||--o{ GAME : participa
    USER {
        string username
        string passwordHash
        object stats
    }
    GAME ||--o{ PLAYER_SESSION : conte
    GAME {
        string name
        string status
        string privateCode
        int maxPlayers
    }
```

## 4. Arquitectura de Microserveis
Disseny de la infraestructura distribuïda.

```mermaid
graph TD
    Unity[Client Unity] --> GW[Gateway Service :3000]
    GW --> Auth[Auth Service :3001]
    GW --> Game[Game Service :3002]
    GW --> Stats[Stats Service :3003]
    
    Auth --> DB[(MongoDB)]
    Game --> DB
    Stats --> DB
```
