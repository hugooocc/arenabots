const IUserRepository = require('../IUserRepository');

class InMemoryUserRepository extends IUserRepository {
    constructor() {
        super();
        this.users = new Map();
        this.currentId = 1;
    }

    async createUser(userData) {
        const id = (this.currentId++).toString();
        const user = { 
            id, 
            ...userData, 
            stats: { mobsKilled: 0, timeSurvived: 0 } 
        };
        this.users.set(id, user);
        return user;
    }

    async findUserByUsername(username) {
        return Array.from(this.users.values()).find(u => u.username === username);
    }

    async findUserById(id) {
        return this.users.get(id);
    }

    async updateStats(userId, mobsKilled, timeSurvived) {
        const user = this.users.get(userId);
        if (!user) throw new Error('User not found');
        
        user.stats.mobsKilled += mobsKilled;
        user.stats.timeSurvived += timeSurvived;
        return user.stats;
    }

    async getAllUsersSorted(limit = 10) {
        return Array.from(this.users.values())
            .sort((a, b) => (b.stats?.mobsKilled || 0) - (a.stats?.mobsKilled || 0))
            .slice(0, limit)
            .map(u => ({
                username: u.username,
                maxMobsKilled: u.stats?.mobsKilled || 0,
                maxTimeSurvived: u.stats?.timeSurvived || 0
            }));
    }
}

module.exports = new InMemoryUserRepository();
