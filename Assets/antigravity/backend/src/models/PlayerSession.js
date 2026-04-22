class PlayerSession {
    constructor(playerId) {
        this.playerId = playerId;
        this.isAlive = true;
        this.lastFireTimestamps = []; // Rolling cache of last 10 firing times
        this.kills = 0;
        this.startTime = Date.now();
        this.position = { x: 0, y: 0 }; // Current position for AI tracking
    }

    addFireTimestamp(timestamp) {
        this.lastFireTimestamps.push(timestamp);
        if (this.lastFireTimestamps.length > 10) {
            this.lastFireTimestamps.shift();
        }
    }

    canFire(newTimestamp) {
        if (!this.isAlive) return false;
        
        // Idempotency: avoid same timestamp
        if (this.lastFireTimestamps.includes(newTimestamp)) return false;

        // Rate limit: 10 shots per 1000ms
        if (this.lastFireTimestamps.length >= 10) {
            const firstOfLastTen = this.lastFireTimestamps[0];
            if (newTimestamp - firstOfLastTen < 1000) {
                return false;
            }
        }
        
        return true;
    }
}

module.exports = PlayerSession;
