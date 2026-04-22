const EnemyAI = require('./EnemyAI');
const { v4: uuidv4 } = require('uuid');

class WaveManager {
    constructor(wss, playersMap) {
        this.wss = wss;
        this.players = playersMap;
        this.activeGames = new Map(); // gameId -> { spawner, targetSync }
    }

    startGame(gameId) {
        if (this.activeGames.has(gameId)) return;

        let waveCount = 1;
        console.log(`[WaveManager] Empezando Hordas y Sincronización en sala ${gameId}`);
        
        // 1. Spawner: Crea enemigos periódicamente
        const spawner = setInterval(() => {
            const numEnemies = Math.min(1 + Math.floor(waveCount / 3), 5);
            for(let i=0; i<numEnemies; i++) {
                const enemyId = uuidv4();
                const side = Math.floor(Math.random() * 4);
                let spawnX = 0, spawnY = 0;
                if (side === 0) { spawnX = (Math.random() * 12) - 6; spawnY = 6; }
                else if (side === 1) { spawnX = 6; spawnY = (Math.random() * 12) - 6; }
                else if (side === 2) { spawnX = (Math.random() * 12) - 6; spawnY = -6; }
                else if (side === 3) { spawnX = -6; spawnY = (Math.random() * 12) - 6; }
                
                EnemyAI.registerEnemy(gameId, enemyId, 100);
                const payload = JSON.stringify({
                    tipo: 'spawn_enemy',
                    enemigoId: enemyId,
                    x: spawnX,
                    y: spawnY,
                    hp: 100
                });
                this.broadcastToRoom(gameId, payload);
            }
            waveCount++;
        }, 5000);

        // 2. TargetSync: Sincroniza a quién ataca cada enemigo (Autoridad del Servidor)
        const targetSync = setInterval(() => {
            const enemies = EnemyAI.rooms.get(gameId);
            if (!enemies || enemies.size === 0) return;

            // Obtener jugadores vivos en esta sala
            const roomPlayers = [];
            this.wss.clients.forEach(c => {
                if (c.readyState === 1 && c.gameId === gameId && c.userId && this.players && this.players.has(c.userId)) {
                    const session = this.players.get(c.userId);
                    if (session.isAlive) roomPlayers.push(session);
                }
            });

            if (roomPlayers.length === 0) return;

            // Para cada enemigo, buscar el jugador más cercano
            const syncData = [];
            enemies.forEach((data, enemyId) => {
                // Posición del enemigo (asumimos que los clientes la tienen, el servidor solo manda el target)
                // Nota: Para un MVP, el servidor no simula el movimiento, solo dicta el objetivo.
                // En una versión más pro, el servidor rastrearía la posición del enemigo.
                // Por ahora, simplemente mandamos el ID del jugador que CADA enemigo debe seguir.
                
                // Selección simple: repartir enemigos o simplemente el más cercano al spawn/última posición conocida.
                // Usaremos una asignación persistente o simplemente el más cercano al azar para este paso.
                const target = roomPlayers[Math.floor(Math.random() * roomPlayers.length)];
                syncData.push({ id: enemyId, targetId: target.playerId });
            });

            const payload = JSON.stringify({
                tipo: 'sync_enemy_targets',
                targets: syncData
            });
            this.broadcastToRoom(gameId, payload);
        }, 1000);

        this.activeGames.set(gameId, { spawner, targetSync });
    }

    broadcastToRoom(gameId, payload) {
        this.wss.clients.forEach((client) => {
            if (client.readyState === 1 && client.gameId === gameId) {
                client.send(payload);
            }
        });
    }

    stopGame(gameId) {
        if (this.activeGames.has(gameId)) {
            const game = this.activeGames.get(gameId);
            clearInterval(game.spawner);
            clearInterval(game.targetSync);
            this.activeGames.delete(gameId);
            EnemyAI.clearRoom(gameId);
            console.log(`[WaveManager] Partida ${gameId} terminada y loops limpiados.`);
        }
    }
}

module.exports = WaveManager;
