require('dotenv').config();
const express = require('express');
const mongoose = require('mongoose');
const userController = require('../../controllers/UserController');
const jwt = require('jsonwebtoken');

const app = express();
app.use(express.json());

const PORT = process.env.STATS_PORT || 3003;
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://127.0.0.1:27017/arena_bots';

mongoose.connect(MONGODB_URI)
    .then(() => console.log(`[Stats Service] Connected to MongoDB`))
    .catch(err => console.error(`[Stats Service] MongoDB error:`, err));

// Auth Middleware (Shared logic)
const authenticate = (req, res, next) => {
    const authHeader = req.headers.authorization;
    if (authHeader) {
        const token = authHeader.split(' ')[1];
        try {
            const decoded = jwt.verify(token, process.env.JWT_SECRET || 'your_super_secret_key_change_in_production');
            req.user = { id: decoded.userId }; 
            next();
        } catch (error) {
            res.status(401).json({ message: 'Sesión caducada o inválida.' });
        }
    } else {
        res.status(401).json({ message: 'No Autorizado: Falta token' });
    }
};

// User Stats Routes
app.get('/api/users/me', authenticate, (req, res) => userController.getStats(req, res));
app.put('/api/users/stats', authenticate, (req, res) => userController.updateStats(req, res));
app.get('/api/users/ranking', authenticate, (req, res) => userController.getRanking(req, res));

app.listen(PORT, () => {
    console.log(`[Stats Service] Listening on port ${PORT}`);
});
