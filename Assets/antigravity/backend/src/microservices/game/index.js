require('dotenv').config();
const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const mongoose = require('mongoose');
const jwt = require('jsonwebtoken');

const Game = require('../../models/Game');
const gameController = require('../../controllers/GameController');
const gameService = require('../../services/GameService');
const WaveManager = require('../../services/WaveManager');
const { handleShoot, handleImpact, players } = require('../../websocket/shootHandler');

const app = express();
app.use(express.json());

const PORT = process.env.GAME_PORT || 3002;
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://127.0.0.1:27017/arena_bots';

mongoose.connect(MONGODB_URI)
    .then(() => {
        console.log(`[Game Service] Connected to MongoDB`);
        // NOTE: We intentionally do NOT wipe existing games on startup.
        // Wiping would cause 500 errors for any player trying to join
        // a game created before a server restart.
    })
    .catch(err => console.error(`[Game Service] MongoDB error:`, err));

const server = http.createServer(app);
const wss = new WebSocket.Server({ server });
const waveManager = new WaveManager(wss);
app.set('wss', wss);

// Auth Middleware
const authenticate = (req, res, next) => {
    const authHeader = req.headers.authorization;
    if (authHeader) {
        const token = authHeader.split(' ')[1];
        try {
            const decoded = jwt.verify(token, process.env.JWT_SECRET || 'your_super_secret_key_change_in_production');
            req.user = { id: decoded.userId }; 
            next();
        } catch (error) {
            res.status(401).json({ message: 'Invalid token' });
        }
    } else {
        res.status(401).json({ message: 'No token' });
    }
};

// Diagnostic endpoint - no auth required - helps verify server has latest code
app.get('/api/games/health', async (req, res) => {
    try {
        const Game = require('../../models/Game');
        const count = await Game.countDocuments();
        res.json({ 
            status: 'ok', 
            version: '2026-04-21-v5',
            dbConnected: true,
            totalGames: count,
            dbName: process.env.MONGODB_URI
        });
    } catch(e) {
        res.json({ status: 'db_error', error: e.message });
    }
});

// Game API Routes
app.post('/api/games', authenticate, (req, res) => gameController.create(req, res));
app.get('/api/games', authenticate, (req, res) => gameController.list(req, res));
app.post('/api/games/:id/join', authenticate, (req, res) => gameController.join(req, res));
app.post('/api/games/join-private', authenticate, (req, res) => gameController.joinPrivate(req, res));
app.post('/api/games/:id/start', authenticate, (req, res) => gameController.start(req, res));

// WebSocket logic (Simplified/Isolated from index.js)
const countdownStates = new Map();

wss.on('connection', async (ws, req) => {
    const url = new URL(req.url, `http://${req.headers.host}`);
    const gameId = url.searchParams.get('gameId');
    const token = url.searchParams.get('token');

    let userId = null;
    if (token) {
        try {
            const decoded = jwt.verify(token, process.env.JWT_SECRET || 'your_super_secret_key_change_in_production');
            userId = decoded.userId;
            ws.userId = userId;
            // Assign username from JWT if present
            if (decoded.username) ws.username = decoded.username;
        } catch (err) {}
    }

    if (gameId) {
        ws.gameId = gameId;
        if (gameId !== 'singleplayer') {
            try {
                const game = await Game.findById(gameId);
                if (!game) { ws.close(4004, "Game not found"); return; }
                const isRegistered = game.players.some(p => p.toString() === userId?.toString());
                if (!isRegistered) { ws.close(4003, "Not registered"); return; }
                // NOTE: start_countdown is now sent in player_ready handler to avoid
                // race condition where client hasn't registered listeners yet.
                console.log(`[WS] Player connected to game ${gameId}. userId: ${userId}`);
            } catch (err) { console.error(err); }
        }
    }

    ws.on('message', async (message) => {
        try {
            const data = JSON.parse(message);
            if (data.tipo === 'player_ready') {
                ws.isReady = true;
                // Assign username from payload as fallback if JWT didn't provide it
                if (!ws.username && data.username) ws.username = data.username;
                const existingPlayers = [];
                wss.clients.forEach(c => {
                    if (c !== ws && c.gameId === ws.gameId && c.isReady) {
                        existingPlayers.push({ userId: c.userId, username: c.username || "Aliado" });
                    }
                });
                if (existingPlayers.length > 0) ws.send(JSON.stringify({ tipo: 'lista_jugadores', jugadores: existingPlayers }));
                wss.clients.forEach(c => {
                    if (c !== ws && c.gameId === ws.gameId) c.send(JSON.stringify({ tipo: 'nuevo_jugador', userId: ws.userId, username: ws.username || "Aliado" }));
                });
                console.log(`[WS] player_ready: ${ws.username || 'unknown'} (${ws.userId}) in game ${ws.gameId}`);

                // Check if all joined players are ready -> send start_countdown
                if (ws.gameId && ws.gameId !== 'singleplayer') {
                    try {
                        const game = await Game.findById(ws.gameId);
                        if (game) {
                            let readyCount = 0;
                            wss.clients.forEach(c => { 
                                if (c.gameId === ws.gameId && c.isReady) readyCount++; 
                            });
                            
                            const joinedCount = game.players.length;
                            console.log(`[WS] Status for game ${ws.gameId}: ${readyCount} players ready out of ${joinedCount} joined (Max: ${game.maxPlayers})`);
                            
                            // Check against joined players, not max players
                            if (readyCount >= joinedCount && joinedCount >= 1) {
                                console.log(`[WS] All joined players are ready in game ${ws.gameId}. Sending start_countdown.`);
                                wss.clients.forEach(c => { 
                                    if (c.gameId === ws.gameId) c.send(JSON.stringify({ tipo: 'start_countdown' })); 
                                });
                            }
                        }
                    } catch (e) { console.error('[WS] Error checking ready state:', e.message); }
                }
            } else if (data.tipo === 'disparo') handleShoot(ws, data, wss);
            else if (data.tipo === 'impacto_proyectil') handleImpact(ws, data, wss);
            else if (data.tipo === 'movimiento') {
                const movePayload = JSON.stringify({ tipo: 'jugador_movido', userId: ws.userId, posicion: data.posicion, velocidad: data.velocidad, mirando: data.mirando });
                wss.clients.forEach(c => { if (c !== ws && c.gameId === ws.gameId) c.send(movePayload); });
            } else if (data.tipo === 'countdown_finished') {
                if (ws.gameId === 'singleplayer') { waveManager.startGame(ws.gameId); return; }
                if (!countdownStates.has(ws.gameId)) countdownStates.set(ws.gameId, new Set());
                const readyPlayers = countdownStates.get(ws.gameId);
                readyPlayers.add(ws.userId);
                const game = await Game.findById(ws.gameId);
                if (readyPlayers.size >= (game ? game.maxPlayers : 1)) {
                    waveManager.startGame(ws.gameId);
                    countdownStates.delete(ws.gameId);
                }
            }
        } catch (e) { console.error(e); }
    });

    ws.on('close', async () => {
        if (ws.gameId) {
            wss.clients.forEach(c => { if (c !== ws && c.gameId === ws.gameId) c.send(JSON.stringify({ tipo: 'jugador_desconectado', userId: ws.userId })); });
            let someoneLeft = false;
            wss.clients.forEach(c => { if (c !== ws && c.gameId === ws.gameId) someoneLeft = true; });
            if (!someoneLeft) {
                // Game Over logic
                waveManager.stopGame(ws.gameId);
                gameService.finishGame(ws.gameId).catch(() => {});
            }
        }
    });
});

server.listen(PORT, () => {
    console.log(`[Game Service] Listening on port ${PORT}`);
});
