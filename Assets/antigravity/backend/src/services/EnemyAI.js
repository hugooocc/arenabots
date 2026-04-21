class EnemyAI {
    constructor() {
        // gameId -> map of enemyId -> { hp, isSpawnedByServer }
        this.rooms = new Map();
    }

    registerEnemy(gameId, enemyId, initialHp) {
        if (!this.rooms.has(gameId)) {
            this.rooms.set(gameId, new Map());
        }
        const enemies = this.rooms.get(gameId);
        // Marcamos como generado por servidor para diferenciarlos de clones locales
        enemies.set(enemyId, { hp: initialHp, isServerSide: true });
        console.log(`[EnemyAI] Registrado enemigo ${enemyId} en sala ${gameId} con ${initialHp} HP.`);
    }

    damageEnemy(gameId, enemyId, damageAmount) {
        if (!this.rooms.has(gameId)) return null;
        
        const enemies = this.rooms.get(gameId);
        
        // Diferenciación de Clones: Solo los enemigos registrados (con ID único en el servidor) procesan daño
        if (!enemies.has(enemyId)) {
            console.warn(`[HitMasking] Impacto rechazado: Enemigo ${enemyId} no existe o es un clon local.`);
            return null;
        }

        const enemy = enemies.get(enemyId);
        
        // Hit Masking: El servidor valida el impacto restando vida real
        enemy.hp -= damageAmount;
        console.log(`[HitMasking] Validado: Enemigo ${enemyId} en ${gameId} recibe ${damageAmount} daño. HP: ${enemy.hp}`);
        
        if (enemy.hp <= 0) {
            enemies.delete(enemyId);
            return 0; // Muerto (el evento enemigo_muerto se emite en el handler)
        }
        return enemy.hp; // Sobrevive
    }

    clearRoom(gameId) {
        this.rooms.delete(gameId);
    }
}

// Singleton export
module.exports = new EnemyAI();
