const GameRepository = require('../repositories/mongo/GameRepository');

class GameService {
  async createGame(name, maxPlayers, ownerId, isPrivate = false) {
    let password = null;
    if (isPrivate) {
      password = Math.random().toString(36).substring(2, 8).toUpperCase();
    }
    const gameData = {
      name,
      maxPlayers: 2, // Hardcoded to 2 as per user request to simplify flow
      password,
      players: [ownerId],
      status: 'WAITING'
    };
    return await GameRepository.createGame(gameData);
  }

  async getAvailableGames() {
    return await GameRepository.findAll();
  }

  async joinGame(gameId, userId, providedPassword = null) {
    const game = await GameRepository.findById(gameId);
    if (!game) {
      throw new Error('Game not found');
    }

    if (game.status !== 'WAITING') {
      throw new Error('Game already started or finished');
    }

    // Validation for private rooms
    if (game.password && game.password !== providedPassword) {
      throw new Error('Invalid room password');
    }

    if (game.players.length >= game.maxPlayers) {
      throw new Error('Game is full');
    }

    // Check if player is already in the game (works for both populated objects and raw ObjectIds)
    const isPlayerInGame = game.players.some(p => (p._id || p).toString() === userId.toString());
    if (isPlayerInGame) {
      return game;
    }

    return await GameRepository.joinGame(gameId, userId);
  }

  async joinGameByCode(code, userId) {
    const game = await GameRepository.findPrivateByCode(code.toUpperCase());
    if (!game) {
      throw new Error('Sala no encontrada o ya ha comenzado');
    }

    if (game.players.length >= game.maxPlayers) {
      throw new Error('Game is full');
    }

    // Check if player is already in the game (works for both populated objects and raw ObjectIds)
    const isPlayerInGame = game.players.some(p => (p._id || p).toString() === userId.toString());
    if (isPlayerInGame) {
      return game;
    }

    const updated = await GameRepository.joinGame(game._id, userId);
    console.log(`[GameService] Player ${userId} joined private game ${game._id} (code: ${code})`);
    return updated;
  }

  async startGame(gameId, userId) {
    const game = await GameRepository.findById(gameId);
    if (!game) {
      throw new Error('Game not found');
    }

    if (game.status !== 'WAITING') {
      throw new Error('Game is not in WAITING status');
    }

    return await GameRepository.updateStatus(gameId, 'PLAYING');
  }

  async finishGame(gameId) {
    console.log(`[GameService] Marcando partida ${gameId} como FINALIZADA.`);
    return await GameRepository.updateStatus(gameId, 'FINISHED');
  }
}

module.exports = new GameService();
