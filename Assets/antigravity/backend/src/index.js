require('dotenv').config();
const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const mongoose = require('mongoose');
const Game = require('./models/Game');

// Connect to MongoDB
mongoose.connect(process.env.MONGODB_URI || 'mongodb://127.0.0.1:27017/arena_bots')
    .then(async () => {
        console.log('Connected to MongoDB');
        // Finalizar TODAS las salas que no estén FINALIZADAS al arrancar el servidor
        try {
            const result = await Game.updateMany(
                { status: { $ne: 'FINISHED' } }, 
                { status: 'FINISHED' }
            );
            if (result.modifiedCount > 0) {
                console.log(`[Startup] ${result.modifiedCount} salas antiguas marcadas como FINALIZADAS.`);
            }
        } catch (err) {
            console.error('[Startup] Error limpiando salas:', err.message);
        }
    })
    .catch(err => console.error('MongoDB connection error:', err));

const { handleShoot, handleImpact, handleDeath, players } = require('./websocket/shootHandler');
const userController = require('./controllers/UserController');
const gameController = require('./controllers/GameController');
const { validateRegistration, validateLogin } = require('./middleware/validation');
const WaveManager = require('./services/WaveManager');
const gameService = require('./services/GameService');

const app = express();
app.use(express.json());

const jwt = require('jsonwebtoken');

// Auth Middleware
const authenticate = (req, res, next) => {
    const authHeader = req.headers.authorization;
    if (authHeader) {
        const token = authHeader.split(' ')[1];
        try {
            // Verificar JWT real
            const decoded = jwt.verify(token, process.env.JWT_SECRET || 'your_super_secret_key_change_in_production');
            req.user = { id: decoded.userId }; 
            next();
        } catch (error) {
            // Si el jugador salta la escena Login, el token será falso o no existirá
            console.error("[Auth] Intento de acceso sin login válido:", error.message);
            res.status(401).json({ message: 'Sesión caducada o inválida. Por favor, inicia sesión.' });
        }
    } else {
        res.status(401).json({ message: 'No Autorizado: Falta token' });
    }
};

const server = http.createServer(app);
const wss = new WebSocket.Server({ server });
const waveManager = new WaveManager(wss, players);

app.set('wss', wss);

const PORT = process.env.PORT || 3000;

// Auth Routes
app.post('/api/auth/register', validateRegistration, (req, res) => userController.register(req, res));
app.post('/api/auth/login', validateLogin, (req, res) => userController.login(req, res));

// User Stats Routes
app.get('/api/users/me', authenticate, (req, res) => userController.getStats(req, res));
app.put('/api/users/stats', authenticate, (req, res) => userController.updateStats(req, res));
app.get('/api/users/ranking', authenticate, (req, res) => userController.getRanking(req, res));

// Game Routes
app.post('/api/games', authenticate, (req, res) => gameController.create(req, res));
app.get('/api/games', authenticate, (req, res) => gameController.list(req, res));
app.post('/api/games/:id/join', authenticate, (req, res) => gameController.join(req, res));
app.post('/api/games/join-private', authenticate, (req, res) => gameController.joinPrivate(req, res));
app.post('/api/games/:id/start', authenticate, (req, res) => gameController.start(req, res));

const countdownStates = new Map(); // gameId -> Set(userIds_finished)

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
        } catch (err) {
            console.error("[WS] Invalid token on connection");
        }
    }

    if (gameId) {
        ws.gameId = gameId;
        console.log(`[WS] Client joined game room: ${gameId}`);

        if (gameId === 'singleplayer') {
            // Modo un jugador: permitimos empezar sin validaciones extras
            // waveManager.startGame(gameId); // Ahora esperaremos al mensaje 'countdown_finished' de todos modos para ser consistentes
        } else {
            // Validar que el usuario está en la lista de jugadores de la sala
            try {
                const game = await Game.findById(gameId);
                if (!game) {
                    console.error(`[WS] Partida ${gameId} no encontrada.`);
                    ws.close(4004, "Game not found");
                    return;
                }

                const isRegistered = game.players.some(p => p.toString() === userId?.toString());
                if (!isRegistered) {
                    console.error(`[WS] Usuario ${userId} no está registrado en la sala ${gameId}. Acceso denegado.`);
                    ws.close(4003, "Not registered in this game");
                    return;
                }

                // Check if everyone is connected
                const requiredCount = game.maxPlayers; // Esperar a que la sala esté LLENA según su capacidad
                let connectedCount = 0;
                wss.clients.forEach(c => {
                    if (c.gameId === gameId && (c.readyState === WebSocket.OPEN || c.readyState === WebSocket.CONNECTING)) {
                        connectedCount++;
                    }
                });

                console.log(`[WS] Sala ${gameId} [${game.name}]: ${connectedCount}/${requiredCount} jugadores presentes.`);

                if (connectedCount >= requiredCount) {
                    console.log(`[WS] ¡SALA LLENA! Emitiendo inicio de cuenta atrás para ${gameId}`);
                    const payload = JSON.stringify({ tipo: 'start_countdown' });
                    wss.clients.forEach(c => {
                        if (c.gameId === gameId && c.readyState === WebSocket.OPEN) {
                            c.send(payload);
                        }
                    });
                } else {
                    console.log(`[WS] Esperando a más jugadores en ${gameId}... (${connectedCount}/${requiredCount})`);
                }
            } catch (err) {
                console.error(`[WS] Error en validación de lobby: ${err.message}`);
            }
        }
    }

    ws.on('message', async (message) => {
        try {
            const data = JSON.parse(message);
            
            if (data.tipo === 'player_ready') {
                const gameId = ws.gameId;
                const userId = ws.userId;
                ws.username = data.username || "Jugador"; // GUARDAR EL USERNAME
                
                // 1. Enviar lista actual al jugador que acaba de decir "estoy listo"
                const existingPlayers = [];
                wss.clients.forEach(c => {
                    if (c !== ws && c.gameId === gameId && c.readyState === WebSocket.OPEN && c.userId && c.isReady) {
                        existingPlayers.push({ userId: c.userId, username: c.username || "Aliado" });
                    }
                });

                if (existingPlayers.length > 0) {
                    ws.send(JSON.stringify({ tipo: 'lista_jugadores', jugadores: existingPlayers }));
                    console.log(`[WS] Enviada lista de ${existingPlayers.length} jugadores a ${userId}`);
                }

                // 2. Notificar a los DEMÁS que este jugador está listo y debe spawnearse
                ws.isReady = true;
                const joinPayload = JSON.stringify({ tipo: 'nuevo_jugador', userId: userId, username: ws.username || "Aliado" });
                wss.clients.forEach(c => {
                    if (c !== ws && c.gameId === gameId && c.readyState === WebSocket.OPEN) {
                        c.send(joinPayload);
                    }
                });
                console.log(`[WS] Notificado nuevo_jugador ${userId} a la sala ${gameId}`);

            } else if (data.tipo === 'disparo') {
                handleShoot(ws, data, wss);
            } else if (data.tipo === 'impacto_proyectil') {
                handleImpact(ws, data, wss);
                const userId = ws.userId;
                if (userId && players.has(userId)) {
                    players.get(userId).position = data.posicion;
                }

                const movePayload = JSON.stringify({
                    tipo: 'jugador_movido',
                    userId: ws.userId,
                    posicion: data.posicion,
                    velocidad: data.velocidad,
                    mirando: data.mirando
                });
                wss.clients.forEach(c => {
                    if (c !== ws && c.gameId === ws.gameId && c.readyState === WebSocket.OPEN) {
                        c.send(movePayload);
                    }
                });
            } else if (data.tipo === 'countdown_finished') {
                const gId = ws.gameId;
                if (!gId) return;

                if (gId === 'singleplayer') {
                    console.log(`[WS] Singleplayer ready. Starting waves.`);
                    waveManager.startGame(gId);
                    return;
                }

                if (ws.userId && players.has(ws.userId)) {
                    players.get(ws.userId).username = ws.username || "Jugador";
                }

                if (!countdownStates.has(gId)) {
                    countdownStates.set(gId, new Set());
                }
                
                const readyPlayers = countdownStates.get(gId);
                readyPlayers.add(ws.userId);

                const game = await Game.findById(gId);
                const requiredReady = game ? game.maxPlayers : 1;

                console.log(`[WS] Jugador ${ws.userId} listo en sala ${gId} (${readyPlayers.size}/${requiredReady})`);

                if (readyPlayers.size >= requiredReady) {
                    console.log(`[WS] Todos listos en sala ${gId}. ¡QUE EMPIECEN LOS BOTS!`);
                    waveManager.startGame(gId);
                    countdownStates.delete(gId);
                }
            } else if (data.tipo === 'player_dead') {
                handleDeath(ws, data, wss);
            }
        } catch (e) {
            console.error(`[WS] Error parseando mensaje: ${e.message}`);
        }
    });

    ws.on('error', (error) => {
        console.error(`[WS] Connection error: ${error.message}`);
    });

    ws.on('close', (code, reason) => {
        console.log(`[WS] Client disconnected. Total clients: ${wss.clients.size}`);
        
        if (ws.gameId) {
            // Notify others
            const leavePayload = JSON.stringify({ tipo: 'jugador_desconectado', userId: ws.userId });
            wss.clients.forEach(c => {
                if (c !== ws && c.gameId === ws.gameId && c.readyState === WebSocket.OPEN) {
                    c.send(leavePayload);
                }
            });

            // Revisa si queda alguien en la sala
            let someoneLeft = false;
            wss.clients.forEach(c => {
                if (c !== ws && c.gameId === ws.gameId && (c.readyState === WebSocket.OPEN || c.readyState === WebSocket.CONNECTING)) {
                    someoneLeft = true;
                }
            });

            if (!someoneLeft) {
                console.log(`[WS] Sala ${ws.gameId} ahora está vacía. Finalizando para limpiar la lista.`);
                waveManager.stopGame(ws.gameId);
                gameService.finishGame(ws.gameId).catch(err => {
                    console.error(`[WS] Error al finalizar partida ${ws.gameId}:`, err.message);
                });
            }
        }
    });
});

server.listen(PORT, () => {
    console.log(`Server listening on port ${PORT}`);
});
