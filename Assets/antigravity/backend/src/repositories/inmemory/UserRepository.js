const IUserRepository = require('../IUserRepository');
const { v4: uuidv4 } = require('uuid');

class InMemoryUserRepository extends IUserRepository {
    constructor() {
        super();
        this.users = new Map();
    }

    async createUser(userData) {
        const id = uuidv4();
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
}

module.exports = new InMemoryUserRepository();
