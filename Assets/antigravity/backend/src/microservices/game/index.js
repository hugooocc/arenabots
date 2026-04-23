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
const waveManager = new WaveManager(wss, players);
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
                const PlayerSession = require('../models/PlayerSession');
                if (userId) {
                    // Always create a fresh session for the new game joined
                    const session = new PlayerSession(userId);
                    session.username = ws.username || "Jugador";
                    players.set(userId, session);
                }
                
                console.log(`[WS] Player connected to game ${gameId}. userId: ${userId}`);
            } catch (err) { console.error(err); }
        }
    }

    ws.on('message', async (rawMessage) => {
        try {
            // Convert Buffer to string safely
            const message = rawMessage.toString();
            const data = JSON.parse(message);
            
            console.log(`[WS] Message from ${ws.username || 'unknown'}:`, data.tipo);

            if (data.tipo === 'player_ready') {
                ws.isReady = true;
                if (!ws.username && data.username) ws.username = data.username;
                if (ws.userId && players.has(ws.userId)) {
                    const session = players.get(ws.userId);
                    session.username = ws.username;
                    // Sincronizar posición inicial enviada por el cliente
                    if (data.posicion) {
                        session.position.x = data.posicion.x;
                        session.position.y = data.posicion.y;
                        console.log(`[WS] Posición inicial para ${ws.username}: (${session.position.x}, ${session.position.y})`);
                    }
                }
                
                console.log(`[WS] ${ws.username} is READY in game ${ws.gameId}`);

                // 1. Sincronizar jugadores existentes
                const existingPlayers = [];
                wss.clients.forEach(c => {
                    if (c !== ws && String(c.gameId) === String(ws.gameId) && c.isReady) {
                        existingPlayers.push({ userId: c.userId, username: c.username || "Aliado" });
                    }
                });
                if (existingPlayers.length > 0) ws.send(JSON.stringify({ tipo: 'lista_jugadores', jugadores: existingPlayers }));
                
                wss.clients.forEach(c => {
                    if (c !== ws && String(c.gameId) === String(ws.gameId)) {
                        c.send(JSON.stringify({ tipo: 'nuevo_jugador', userId: ws.userId, username: ws.username || "Aliado" }));
                    }
                });

                // 2. Si la partida YA EMPEZÓ, enviar FULL_STATE al nuevo (Reconexión / Entrada Tardía)
                if (waveManager.activeGames.has(ws.gameId)) {
                    const roomSnapshot = EnemyAI.getRoomSnapshot(ws.gameId);
                    ws.send(JSON.stringify({
                        tipo: 'full_state',
                        enemies: roomSnapshot,
                        tick: waveManager.activeGames.get(ws.gameId).serverTick
                    }));
                }

                // Trigger countdown logic
                if (ws.gameId && ws.gameId !== 'singleplayer' && !waveManager.activeGames.has(ws.gameId)) {
                    const game = await Game.findById(ws.gameId);
                    if (game) {
                        let readyCount = 0;
                        const connectedClients = [];
                        wss.clients.forEach(c => { 
                            if (String(c.gameId) === String(ws.gameId)) {
                                connectedClients.push({ user: c.username, ready: !!c.isReady });
                                if (c.isReady) readyCount++; 
                            }
                        });
                        
                        const joinedCount = game.players.length;
                        if (readyCount >= joinedCount && joinedCount >= 2) {
                            wss.clients.forEach(c => { 
                                if (String(c.gameId) === String(ws.gameId)) c.send(JSON.stringify({ tipo: 'start_countdown' })); 
                            });
                        }
                    }
                }
            } else if (data.tipo === 'disparo') handleShoot(ws, data, wss);
            else if (data.tipo === 'impacto_proyectil') handleImpact(ws, data, wss);
            else if (data.tipo === 'player_dead') {
                const { handleDeath } = require('../../websocket/shootHandler');
                handleDeath(ws, data, wss, waveManager);
            }
            else if (data.tipo === 'movimiento') {
                if (ws.userId && players.has(ws.userId)) {
                    const p = players.get(ws.userId);
                    const input = data.input || { x: 0, y: 0 };
                    const seq = data.seq || 0;

                    // Server Authoritative Movement Calculation (Speed 5.0, Step 0.05)
                    const moveSpeed = 5.0;
                    const moveDelta = 0.05; 
                    
                    let dx = input.x;
                    let dy = input.y;
                    const magnitude = Math.sqrt(dx * dx + dy * dy);
                    
                    if (magnitude > 0.01) {
                        dx = (dx / magnitude) * moveSpeed * moveDelta;
                        dy = (dy / magnitude) * moveSpeed * moveDelta;
                        
                        p.position.x += dx;
                        p.position.y += dy;
                    }

                    // Broadcast "player_update" to ALL in room (including self for reconciliation)
                    const payload = JSON.stringify({
                        tipo: 'player_update',
                        userId: ws.userId,
                        pos: p.position,
                        seq: seq
                    });

                    wss.clients.forEach(c => {
                        if (c.readyState === 1 && String(c.gameId) === String(ws.gameId)) {
                            c.send(payload);
                        }
                    });
                }
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
