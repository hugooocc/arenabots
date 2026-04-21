const userService = require('../services/UserService');

class UserController {
    async register(req, res) {
        try {
            const { username, password } = req.body;

            // Simple validation
            if (!username || !password || password.length < 6) {
                return res.status(400).json({ 
                    message: "Invalid username or password (min 6 characters)" 
                });
            }

            const user = await userService.register(username, password);
            
            res.status(201).json({
                message: "Usuario registrado exitosamente",
                userId: user.id
            });
        } catch (error) {
            const status = error.status || 500;
            res.status(status).json({ message: error.message });
        }
    }

    async login(req, res) {
        try {
            const { username, password } = req.body;

            if (!username || !password) {
                return res.status(400).json({ message: "Username and password are required" });
            }

            const { token, user } = await userService.login(username, password);

            res.status(200).json({
                message: "Login exitoso",
                token,
                user
            });
        } catch (error) {
            const status = error.status || 500;
            res.status(status).json({ message: error.message });
        }
    }

    async getStats(req, res) {
        try {
            const userId = req.user.id;
            const stats = await userService.getUserStats(userId);
            res.status(200).json(stats);
        } catch (error) {
            const status = error.status || 500;
            res.status(status).json({ message: error.message });
        }
    }

    async updateStats(req, res) {
        try {
            const userId = req.user.id;
            const { mobsKilled, timeSurvived } = req.body;

            if (mobsKilled === undefined || timeSurvived === undefined) {
                return res.status(400).json({ message: "mobsKilled and timeSurvived are required" });
            }

            const result = await userService.updateUserStats(userId, Number(mobsKilled), Number(timeSurvived));
            res.status(200).json(result);
        } catch (error) {
            const status = error.status || 500;
            res.status(status).json({ message: error.message });
        }
    }
}

module.exports = new UserController();
