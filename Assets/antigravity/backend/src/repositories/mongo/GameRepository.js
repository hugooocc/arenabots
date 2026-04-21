const IGameRepository = require('../IGameRepository');
const Game = require('../../models/Game');
require('../../models/User'); // Required so Mongoose knows the User schema for populate()

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
    // Accept WAITING or PLAYING in case host already entered the arena
    const game = await Game.findOne({
      password: code,
      status: { $in: ['WAITING', 'PLAYING'] }
    }).populate('players', 'username');
    console.log(`[GameRepository] findPrivateByCode('${code}') -> ${game ? game._id : 'NOT FOUND'}`);
    return game;
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

