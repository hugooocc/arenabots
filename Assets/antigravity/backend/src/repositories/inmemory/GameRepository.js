const IGameRepository = require('../IGameRepository');

class InMemoryGameRepository extends IGameRepository {
    constructor() {
        super();
        this.games = new Map();
        this.currentId = 1;
    }

    async createGame(gameData) {
        const id = (this.currentId++).toString();
        const game = { 
            id, 
            ...gameData, 
            players: gameData.players || [],
            status: gameData.status || 'WAITING' 
        };
        this.games.set(id, game);
        return game;
    }

    async findById(id) {
        return this.games.get(id);
    }

    async findAll() {
        return Array.from(this.games.values());
    }

    async joinGame(gameId, userId) {
        const game = this.games.get(gameId);
        if (!game) throw new Error('Game not found');
        if (!game.players.includes(userId)) {
            game.players.push(userId);
        }
        return game;
    }

    async updateStatus(gameId, status) {
        const game = this.games.get(gameId);
        if (!game) throw new Error('Game not found');
        game.status = status;
        return game;
    }

    async findPrivateByCode(code) {
        return Array.from(this.games.values()).find(g => g.privateCode === code);
    }
}

module.exports = new InMemoryGameRepository();
