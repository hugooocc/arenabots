const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const PlayerSession = require('../models/PlayerSession');

const players = new Map();

/**
 * Manejador WebSocket para eventos de disparo.
 * Sigue la especificación en specs/shooting-system/spec.md
 */
function handleShoot(ws, data, wss) {
    const { tipo, jugadorId, partidaId, posicion, direccion, timestamp } = data;

    // 1.1 Escuchar mensajes de tipo "disparo"
    if (tipo !== 'disparo') return;

    // Validación de campos obligatorios
    if (!jugadorId || !partidaId || !posicion || !direccion || !timestamp) {
        console.warn(`[WS] Payload de disparo incompleto de ${jugadorId}`);
        return;
    }

    // Obtener o crear sesión del jugador (simulado para este paso)
    if (!players.has(jugadorId)) {
        players.set(jugadorId, new PlayerSession(jugadorId));
    }
    const session = players.get(jugadorId);

    // 1.3 Validar que el jugador está vivo (isAlive)
    if (!session.isAlive) {
        console.warn(`[WS] Disparo bloqueado: jugador muerto ${jugadorId}`);
        return;
    }

    // 1.6 Aplicar límite de tasa: máx. 10 eventos/seg por jugador
    // También maneja el caso límite de eventos duplicados (mismo timestamp)
    if (!session.canFire(timestamp)) {
        console.warn(`[WS] Disparo bloqueado: límite de tasa o duplicado para ${jugadorId}`);
        return;
    }

    // 1.4 Validar que la dirección es un vector normalizado (módulo ~1.0 ± 0.05)
    const magnitud = Math.sqrt(direccion.x * direccion.x + direccion.y * direccion.y);
    if (magnitud < 0.95 || magnitud > 1.05) {
        console.warn(`[WS] Disparo bloqueado: vector no normalizado mag=${magnitud} de ${jugadorId}`);
        return;
    }

    // 1.5 Validar que la posición está dentro de los límites del mapa (Ej: 0-100 para este MVP)
    if (posicion.x < -50 || posicion.x > 50 || posicion.y < -50 || posicion.y > 50) {
        console.warn(`[WS] Disparo bloqueado: posición fuera de límites (${posicion.x}, ${posicion.y})`);
        return;
    }

    // Registrar el disparo exitoso en la sesión
    session.addFireTimestamp(timestamp);

    // 1.7 Generar un proyectilId único (uuid)
    const proyectilId = uuidv4();

    // 1.8 Retransmitir disparo_retransmision a todos los clientes
    const retransmision = {
        tipo: 'disparo_retransmision',
        jugadorId,
        posicion: {
            x: Number(posicion.x.toFixed(2)),
            y: Number(posicion.y.toFixed(2))
        },
        direccion: {
            x: Number(direccion.x.toFixed(2)),
            y: Number(direccion.y.toFixed(2))
        },
        proyectilId
    };

    const payload = JSON.stringify(retransmision);
    wss.clients.forEach((client) => {
        if (client.readyState === WebSocket.OPEN && client.gameId === partidaId) {
            client.send(payload);
        }
    });

    console.log(`[WS] Disparo validado y retransmitido: ${proyectilId} de ${jugadorId}`);
}

const EnemyAI = require('../services/EnemyAI');

function handleImpact(ws, data, wss) {
    const { tipo, partidaId, enemigoId, dano } = data;

    if (tipo !== 'impacto_proyectil') return;

    try {
        const nuevoHp = EnemyAI.damageEnemy(partidaId, enemigoId, dano);

        if (nuevoHp !== null && typeof nuevoHp === 'number') {
            if (nuevoHp <= 0) {
                const killerId = ws.userId;
                if (killerId && players.has(killerId)) {
                    players.get(killerId).kills++;
                }

                const deathPayload = JSON.stringify({
                    tipo: 'enemigo_muerto',
                    enemigoId: enemigoId
                });
                
                wss.clients.forEach(client => {
                    if (client.readyState === WebSocket.OPEN && client.gameId === partidaId) {
                        client.send(deathPayload);
                    }
                });
            }
        }
    } catch (e) {
        console.error("[WS] Error confirmando impacto:", e.message);
    }
}

function handleDeath(ws, data, wss, waveManager) {
    const { tipo, partidaId } = data;
    if (tipo !== 'player_dead') return;

    const userId = ws.userId;
    if (!userId || !players.has(userId)) return;

    const session = players.get(userId);
    if (!session.isAlive) return;

    session.isAlive = false;
    console.log(`[WS] Jugador ${userId} ha muerto en partida ${partidaId}`);

    const targetGameId = String(partidaId || ws.gameId);
    
    // Informar a los demás
    const deathPayload = JSON.stringify({ tipo: 'jugador_muerto', userId });
    wss.clients.forEach(c => {
        if (c.readyState === WebSocket.OPEN && String(c.gameId) === targetGameId) {
            c.send(deathPayload);
        }
    });

    // Comprobar si todos han muerto en esta partida específica
    const roomSessions = [];
    wss.clients.forEach(c => {
        if (String(c.gameId) === targetGameId && c.userId && players.has(c.userId)) {
            roomSessions.push(players.get(c.userId));
        }
    });

    const aliveInRoom = roomSessions.filter(p => p.isAlive);
    console.log(`[WS] Muerte procesada. Jugadores en sala ${targetGameId}: ${roomSessions.length}, Vivos: ${aliveInRoom.length}`);
    
    if (aliveInRoom.length === 0 && roomSessions.length > 0) {
        // PROTECCIÓN: Solo enviar Game Over una vez por sala
        if (session.gameOverTriggered) return;
        roomSessions.forEach(s => s.gameOverTriggered = true);

        console.log(`[WS] ¡TODOS MUERTOS en ${targetGameId}! Enviando estadísticas finales.`);
        
        const roomStats = roomSessions.map(p => ({
            userId: p.playerId,
            username: p.username || "Jugador",
            kills: p.kills,
            time: Math.floor((Date.now() - p.startTime) / 1000)
        }));

        const gameOverPayload = JSON.stringify({
            tipo: 'game_over',
            stats: roomStats
        });

        wss.clients.forEach(c => {
            if (String(c.gameId) === targetGameId && c.readyState === WebSocket.OPEN) {
                c.send(gameOverPayload);
            }
        });

        // Detener lógica de hordas y marcar en DB
        if (waveManager) waveManager.stopGame(targetGameId);
        
        const gameService = require('../services/GameService');
        gameService.finishGame(targetGameId).catch(err => console.error(err));
    }
}

module.exports = { handleShoot, handleImpact, handleDeath, players };
