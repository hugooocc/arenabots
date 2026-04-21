const EnemyAI = require('./EnemyAI');
const { v4: uuidv4 } = require('uuid');

class WaveManager {
    constructor(wss) {
        this.wss = wss;
        this.activeGames = new Map(); // gameId -> intervalId
    }

    startGame(gameId) {
        if (this.activeGames.has(gameId)) return;

        let waveCount = 1;
        console.log(`[WaveManager] Empezando Hordas en sala ${gameId}`);
        
        // Spawnear enemigos cada 2.5 segundos
        const spawner = setInterval(() => {
            const numEnemies = Math.min(1 + Math.floor(waveCount / 3), 5); // Escala más suave
            
            for(let i=0; i<numEnemies; i++) {
                const enemyId = uuidv4();
                
                // Generar posición aleatoria en los bordes de un área de 6x6 (visible con cámara de size 5)
                const side = Math.floor(Math.random() * 4);
                let spawnX = 0, spawnY = 0;
                
                if (side === 0) { spawnX = (Math.random() * 12) - 6; spawnY = 6; }
                else if (side === 1) { spawnX = 6; spawnY = (Math.random() * 12) - 6; }
                else if (side === 2) { spawnX = (Math.random() * 12) - 6; spawnY = -6; }
                else if (side === 3) { spawnX = -6; spawnY = (Math.random() * 12) - 6; }
                
                // Registrar enemigo en IA para que tenga vida base (100)
                EnemyAI.registerEnemy(gameId, enemyId, 100);

                // Notificar a Unity
                const payload = JSON.stringify({
                    tipo: 'spawn_enemy',
                    enemigoId: enemyId,
                    x: spawnX,
                    y: spawnY,
                    hp: 100
                });

                this.wss.clients.forEach((client) => {
                    if (client.readyState === 1 && client.gameId === gameId) {
                        client.send(payload);
                    }
                });
            }
            waveCount++;
        }, 5000);

        this.activeGames.set(gameId, spawner);
    }

    stopGame(gameId) {
        if (this.activeGames.has(gameId)) {
            clearInterval(this.activeGames.get(gameId));
            this.activeGames.delete(gameId);
            EnemyAI.clearRoom(gameId);
            console.log(`[WaveManager] Partida ${gameId} terminada.`);
        }
    }
}

module.exports = WaveManager;
