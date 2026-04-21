const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const userRepository = require('../repositories/mongo/UserRepository');

class UserService {
    async register(username, password) {
        const lowerUsername = username.toLowerCase();
        // Check if user exists
        const existingUser = await userRepository.findUserByUsername(lowerUsername);
        if (existingUser) {
            const error = new Error('Username already exists');
            error.status = 409;
            throw error;
        }

        // Hash password
        const saltRounds = 10;
        const passwordHash = await bcrypt.hash(password, saltRounds);

        const user = await userRepository.createUser({
            username: lowerUsername,
            passwordHash
        });

        console.log(`[Auth] New user registered: ${lowerUsername}`);

        return {
            id: user._id,
            username: user.username
        };
    }

    async login(username, password) {
        const lowerUsername = username.toLowerCase();
        // Find user
        const user = await userRepository.findUserByUsername(lowerUsername);
        if (!user) {
            console.warn(`[Auth] Login failed: User not found -> ${lowerUsername}`);
            const error = new Error('Invalid username or password');
            error.status = 401;
            throw error;
        }

        // Check password
        const isPasswordValid = await bcrypt.compare(password, user.passwordHash);
        if (!isPasswordValid) {
            console.warn(`[Auth] Login failed: Incorrect password -> ${lowerUsername}`);
            const error = new Error('Invalid username or password');
            error.status = 401;
            throw error;
        }

        // Generate JWT
        const token = jwt.sign(
            { userId: user._id, username: user.username },
            process.env.JWT_SECRET || 'your_super_secret_key_change_in_production',
            { expiresIn: '24h' }
        );

        console.log(`[Auth] User logged in: ${lowerUsername}`);

        return {
            token,
            user: {
                id: user._id,
                username: user.username
            }
        };
    }

    async getUserStats(userId) {
        const user = await userRepository.findUserById(userId);
        if (!user) {
            const error = new Error('User not found');
            error.status = 404;
            throw error;
        }

        return {
            username: user.username,
            maxMobsKilled: user.maxMobsKilled || 0,
            maxTimeSurvived: user.maxTimeSurvived || 0
        };
    }

    async updateUserStats(userId, mobsKilled, timeSurvived) {
        const user = await userRepository.findUserById(userId);
        if (!user) {
            const error = new Error('User not found');
            error.status = 404;
            throw error;
        }

        let updated = false;

        if (mobsKilled > (user.maxMobsKilled || 0)) {
            user.maxMobsKilled = mobsKilled;
            updated = true;
        }

        if (timeSurvived > (user.maxTimeSurvived || 0)) {
            user.maxTimeSurvived = timeSurvived;
            updated = true;
        }

        if (updated) {
            await user.save();
        }

        return {
            updated,
            maxMobsKilled: user.maxMobsKilled,
            maxTimeSurvived: user.maxTimeSurvived
        };
    }
}

module.exports = new UserService();
