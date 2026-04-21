const GameRepository = require('../repositories/mongo/GameRepository');

class GameService {
  async createGame(name, maxPlayers, ownerId, isPrivate = false) {
    let password = null;
    if (isPrivate) {
      password = Math.random().toString(36).substring(2, 8).toUpperCase();
    }
    const gameData = {
      name,
      maxPlayers,
      password,
      players: [ownerId],
      status: 'WAITING'
    };
    return await GameRepository.createGame(gameData);
  }

  async getAvailableGames() {
    return await GameRepository.findAvailableGames();
  }

  async joinGame(gameId, userId, providedPassword = null) {
    const game = await GameRepository.findGameById(gameId);
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

    // Check if player is already in the game
    const isPlayerInGame = game.players.some(p => p._id.toString() === userId.toString());
    if (isPlayerInGame) {
      return game;
    }

    return await GameRepository.addPlayerToGame(gameId, userId);
  }

  async joinGameByCode(code, userId) {
    const game = await GameRepository.findGameByCode(code.toUpperCase());
    if (!game) {
      throw new Error('Sala no encontrada o ya ha comenzado');
    }

    if (game.players.length >= game.maxPlayers) {
      throw new Error('Game is full');
    }

    // Check if player is already in the game
    const isPlayerInGame = game.players.some(p => p._id.toString() === userId.toString());
    if (isPlayerInGame) {
      return game;
    }

    return await GameRepository.addPlayerToGame(game._id, userId);
  }

  async startGame(gameId, userId) {
    const game = await GameRepository.findGameById(gameId);
    if (!game) {
      throw new Error('Game not found');
    }

    // Optional: Check if userId is the owner (the first player in the array)
    // For now, any player can start it or we can just let it be open
    
    if (game.status !== 'WAITING') {
      throw new Error('Game is not in WAITING status');
    }

    return await GameRepository.updateGameStatus(gameId, 'PLAYING');
  }

  async finishGame(gameId) {
    console.log(`[GameService] Marcando partida ${gameId} como FINALIZADA.`);
    return await GameRepository.updateGameStatus(gameId, 'FINISHED');
  }
}

module.exports = new GameService();
