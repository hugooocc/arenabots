class EnemyAI {
    constructor() {
        // gameId -> map of enemyId -> { hp, x, y, seq, isServerSide }
        this.rooms = new Map();
    }

    registerEnemy(gameId, enemyId, initialHp, x = 0, y = 0) {
        if (!this.rooms.has(gameId)) {
            this.rooms.set(gameId, new Map());
        }
        const enemies = this.rooms.get(gameId);
        enemies.set(enemyId, { 
            hp: initialHp, 
            x: x, 
            y: y, 
            seq: 1,
            isServerSide: true 
        });
        console.log(`[EnemyAI] Registrado enemigo ${enemyId} en sala ${gameId} en (${x}, ${y}) con ${initialHp} HP.`);
    }

    updateEnemyPosition(gameId, enemyId, x, y) {
        if (!this.rooms.has(gameId)) return;
        const enemies = this.rooms.get(gameId);
        if (enemies.has(enemyId)) {
            const enemy = enemies.get(enemyId);
            enemy.x = x;
            enemy.y = y;
            enemy.seq++;
        }
    }

    damageEnemy(gameId, enemyId, damageAmount) {
        if (!this.rooms.has(gameId)) return null;
        const enemies = this.rooms.get(gameId);
        if (!enemies.has(enemyId)) return null;

        const enemy = enemies.get(enemyId);
        enemy.hp -= damageAmount;
        enemy.seq++;
        
        console.log(`[HitMasking] Enemigo ${enemyId} en ${gameId} recibe ${damageAmount} daño. HP: ${enemy.hp}`);
        
        if (enemy.hp <= 0) {
            enemies.delete(enemyId);
            return 0;
        }
        return enemy.hp;
    }

    getRoomSnapshot(gameId) {
        if (!this.rooms.has(gameId)) return [];
        const enemies = this.rooms.get(gameId);
        const snapshot = [];
        enemies.forEach((data, id) => {
            snapshot.push({
                id: id,
                x: Number(data.x.toFixed(2)),
                y: Number(data.y.toFixed(2)),
                hp: data.hp,
                seq: data.seq
            });
        });
        return snapshot;
    }

    clearRoom(gameId) {
        this.rooms.delete(gameId);
    }
}

module.exports = new EnemyAI();
