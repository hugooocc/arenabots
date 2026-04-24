const EnemyAI = require('./EnemyAI');
const { v4: uuidv4 } = require('uuid');

class WaveManager {
    constructor(wss, playersMap) {
        this.wss = wss;
        this.players = playersMap;
        this.activeGames = new Map(); // gameId -> { intervals, serverTick, startTime }
    }

    startGame(gameId) {
        if (this.activeGames.has(gameId)) return;

        console.log(`[WaveManager] Iniciando partida autoritativa en sala ${gameId}`);
        
        const gameData = {
            serverTick: 0,
            startTime: Date.now(),
            waveCount: 1,
            intervals: [],
            gameOverTriggered: false
        };

        // 1. Spawner de Enemigos (cada 5s)
        const spawner = setInterval(() => {
            const numEnemies = Math.min(1 + Math.floor(gameData.waveCount / 3), 5);
            for(let i=0; i<numEnemies; i++) {
                const enemyId = uuidv4();
                const side = Math.floor(Math.random() * 4);
                let spawnX = 0, spawnY = 0;
                if (side === 0) { spawnX = (Math.random() * 12) - 6; spawnY = 6; }
                else if (side === 1) { spawnX = 6; spawnY = (Math.random() * 12) - 6; }
                else if (side === 2) { spawnX = (Math.random() * 12) - 6; spawnY = -6; }
                else if (side === 3) { spawnX = -6; spawnY = (Math.random() * 12) - 6; }
                
                EnemyAI.registerEnemy(gameId, enemyId, 100, spawnX, spawnY);
                this.broadcastToRoom(gameId, JSON.stringify({
                    tipo: 'spawn_enemy',
                    enemigoId: enemyId,
                    x: spawnX,
                    y: spawnY,
                    hp: 100
                }));
            }
            gameData.waveCount++;
        }, 5000);

        // 2. Tick Loop Principal (20Hz - Sincronización de Estado e IA)
        const tickLoop = setInterval(() => {
            gameData.serverTick++;
            const currentTime = (Date.now() - gameData.startTime) / 1000;

            // Simple IA: Mover enemigos hacia el jugador más cercano
            this.updateEnemiesAI(gameId);

            // Broadcast de Estado (Sync Time + Enemies)
            const snapshot = EnemyAI.getRoomSnapshot(gameId);
            const payload = JSON.stringify({
                tipo: 'game_tick',
                tick: gameData.serverTick,
                time: currentTime,
                enemies: snapshot
            });
            
            this.broadcastToRoom(gameId, payload);
        }, 50);

        gameData.intervals.push(spawner, tickLoop);
        this.activeGames.set(gameId, gameData);
    }

    updateEnemiesAI(gameId) {
        const gameData = this.activeGames.get(gameId);
        if (!gameData) return;

        const enemies = EnemyAI.rooms.get(gameId);
        if (!enemies || enemies.size === 0) return;

        // Obtener jugadores vivos para targets
        const roomPlayers = [];
        const targetId = String(gameId);
        let clientsChecked = 0;
        let matchedClients = 0;

        this.wss.clients.forEach(c => {
            clientsChecked++;
            const clientGameId = String(c.gameId || "");
            if (c.readyState === 1 && clientGameId === targetId && c.userId && this.players.has(c.userId)) {
                matchedClients++;
                const p = this.players.get(c.userId);
                if (p && p.isAlive) roomPlayers.push(p);
            }
        });

        if (roomPlayers.length === 0) {
            if (gameData.serverTick % 100 === 0) { // Log every 5 seconds (100 ticks * 50ms)
                console.log(`[DEBUG-AI] Sala ${targetId}: 0 jugadores válidos. (Clientes: ${clientsChecked}, Match: ${matchedClients})`);
            }
            return;
        }

        enemies.forEach((data, id) => {
            // Encontrar jugador más cercano
            let nearest = null;
            let minDist = 999;
            roomPlayers.forEach(p => {
                const d = Math.sqrt(Math.pow(p.position.x - data.x, 2) + Math.pow(p.position.y - data.y, 2));
                if (d < minDist) { minDist = d; nearest = p; }
            });

            if (nearest) {
                // Mover hacia el jugador (Velocidad simple constante 2 unidades/seg -> 0.1 por tick)
                const dx = nearest.position.x - data.x;
                const dy = nearest.position.y - data.y;
                const angle = Math.atan2(dy, dx);
                
                // Normalizar y aplicar paso (2.0 speed * 0.05 step = 0.1)
                const vx = Math.cos(angle) * 0.1;
                const vy = Math.sin(angle) * 0.1;
                
                EnemyAI.updateEnemyPosition(gameId, id, data.x + vx, data.y + vy);
            }
        });
    }

    broadcastToRoom(gameId, payload) {
        this.wss.clients.forEach((client) => {
            if (client.readyState === 1 && String(client.gameId) === String(gameId)) {
                client.send(payload);
            }
        });
    }

    stopGame(gameId) {
        if (this.activeGames.has(gameId)) {
            const game = this.activeGames.get(gameId);
            game.intervals.forEach(clearInterval);
            this.activeGames.delete(gameId);
            EnemyAI.clearRoom(gameId);
            console.log(`[WaveManager] Partida ${gameId} terminada.`);
        }
    }
}

module.exports = WaveManager;
