class Projectile {
    constructor(id, ownerId, startPosition, direction, speed, spawnTimestamp) {
        this.id = id;
        this.ownerId = ownerId;
        this.startPosition = startPosition; // {x, y} rounded to 2 decimals
        this.direction = direction; // normalized {x, y}
        this.speed = speed;
        this.spawnTimestamp = spawnTimestamp;
    }
}

module.exports = Projectile;
