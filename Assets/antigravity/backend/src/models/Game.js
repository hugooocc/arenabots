const mongoose = require('mongoose');

const GameSchema = new mongoose.Schema({
  name: {
    type: String,
    required: true,
    trim: true
  },
  maxPlayers: {
    type: Number,
    required: true,
    min: 2,
    max: 8
  },
  players: [{
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User'
  }],
  status: {
    type: String,
    enum: ['WAITING', 'PLAYING', 'FINISHED'],
    default: 'WAITING'
  },
  password: {
    type: String,
    default: null
  },
  createdAt: {
    type: Date,
    default: Date.now
  }
});

module.exports = mongoose.model('Game', GameSchema);
