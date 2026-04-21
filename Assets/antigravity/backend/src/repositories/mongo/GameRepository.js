const IGameRepository = require('../IGameRepository');
const Game = require('../../models/Game');

class GameRepository extends IGameRepository {
  async createGame(gameData) {
    const game = new Game(gameData);
    return await game.save();
  }

  async findById(gameId) {
    return await Game.findById(gameId).populate('players', 'username');
  }

  async findAll() {
    return await Game.find({ status: 'WAITING', password: null })
      .populate('players', 'username')
      .sort({ createdAt: -1 });
  }

  async findPrivateByCode(code) {
    return await Game.findOne({ status: 'WAITING', password: code }).populate('players', 'username');
  }

  async updateStatus(gameId, status) {
    return await Game.findByIdAndUpdate(
      gameId,
      { status },
      { new: true }
    );
  }

  async joinGame(gameId, userId) {
    return await Game.findByIdAndUpdate(
      gameId,
      { $addToSet: { players: userId } },
      { new: true }
    ).populate('players', 'username');
  }
}

module.exports = new GameRepository();

