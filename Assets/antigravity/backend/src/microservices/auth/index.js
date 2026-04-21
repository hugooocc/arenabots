require('dotenv').config();
const express = require('express');
const mongoose = require('mongoose');
const userController = require('../../controllers/UserController');
const { validateRegistration, validateLogin } = require('../../middleware/validation');

const app = express();
app.use(express.json());

const PORT = process.env.AUTH_PORT || 3001;
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://127.0.0.1:27017/arena_bots';

mongoose.connect(MONGODB_URI)
    .then(() => console.log(`[Auth Service] Connected to MongoDB`))
    .catch(err => console.error(`[Auth Service] MongoDB error:`, err));

// Auth Routes
app.post('/api/auth/register', validateRegistration, (req, res) => userController.register(req, res));
app.post('/api/auth/login', validateLogin, (req, res) => userController.login(req, res));

app.listen(PORT, () => {
    console.log(`[Auth Service] Listening on port ${PORT}`);
});
