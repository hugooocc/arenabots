/**
 * Interface for Game Repository
 * This defines the contract that all Game implementations must follow.
 */
class IGameRepository {
    async createGame(gameData) { throw new Error('Method not implemented'); }
    async findById(id) { throw new Error('Method not implemented'); }
    async findAll() { throw new Error('Method not implemented'); }
    async joinGame(gameId, userId) { throw new Error('Method not implemented'); }
    async updateStatus(gameId, status) { throw new Error('Method not implemented'); }
    async findPrivateByCode(code) { throw new Error('Method not implemented'); }
}

module.exports = IGameRepository;
