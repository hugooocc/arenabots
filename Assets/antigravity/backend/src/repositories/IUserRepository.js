/**
 * Interface for User Repository
 * This defines the contract that all User implementations must follow.
 */
class IUserRepository {
    async createUser(userData) { throw new Error('Method not implemented'); }
    async findUserByUsername(username) { throw new Error('Method not implemented'); }
    async findUserById(id) { throw new Error('Method not implemented'); }
    async updateStats(userId, mobsKilled, timeSurvived) { throw new Error('Method not implemented'); }
}

module.exports = IUserRepository;
