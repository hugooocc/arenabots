const mongoose = require('mongoose');

const userSchema = new mongoose.Schema({
    username: {
        type: String,
        required: true,
        unique: true,
        trim: true,
        lowercase: true
    },
    passwordHash: {
        type: String,
        required: true
    },
    createdAt: {
        type: Date,
        default: Date.now
    },
    maxMobsKilled: {
        type: Number,
        default: 0
    },
    maxTimeSurvived: {
        type: Number,
        default: 0
    }
});

module.exports = mongoose.model('User', userSchema);
