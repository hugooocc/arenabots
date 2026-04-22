const IUserRepository = require('../IUserRepository');
const User = require('../../models/User');

class UserRepository extends IUserRepository {
    async createUser(userData) {
        const user = new User(userData);
        return await user.save();
    }

    async findUserByUsername(username) {
        return await User.findOne({ username });
    }

    async findUserById(id) {
        return await User.findById(id);
    }

    async updateStats(userId, mobsKilled, timeSurvived) {
        return await User.findByIdAndUpdate(
            userId,
            { 
                $inc: { 
                    'stats.mobsKilled': mobsKilled, 
                    'stats.timeSurvived': timeSurvived 
                } 
            },
            { new: true }
        );
    }

    async getAllUsersSorted(limit = 10) {
        return await User.find()
            .sort({ maxMobsKilled: -1 })
            .limit(limit)
            .select('username maxMobsKilled maxTimeSurvived');
    }
}

module.exports = new UserRepository();

