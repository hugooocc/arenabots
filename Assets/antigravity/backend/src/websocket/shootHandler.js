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
    console.log(`[DEBUG-GAMEOVER] Recibido player_dead. ws.userId: ${userId}, partidaId: ${partidaId}`);
    
    if (!userId || !players.has(userId)) {
        console.log(`[DEBUG-GAMEOVER] Usuario no encontrado en el mapa 'players'. userId: ${userId}, Total en mapa: ${players.size}`);
        if (userId) console.log("[DEBUG-GAMEOVER] Claves en mapa:", Array.from(players.keys()));
        return;
    }

    const session = players.get(userId);
    if (!session.isAlive) {
        console.log(`[DEBUG-GAMEOVER] El usuario ${userId} ya estaba marcado como muerto.`);
        return;
    }

    session.isAlive = false;
    console.log(`[DEBUG-GAMEOVER] Marcado usuario ${userId} como MUERTO. isAlive ahora es ${session.isAlive}`);

    const targetGameId = String(partidaId || ws.gameId);
    
    // Informar a los demás
    const deathPayload = JSON.stringify({ tipo: 'jugador_muerto', userId });
    wss.clients.forEach(c => {
        if (c.readyState === WebSocket.OPEN && String(c.gameId) === targetGameId) {
            c.send(deathPayload);
        }
    });

    // Comprobar si todos han muerto en esta partida específica
    const searchId = String(targetGameId);
    const roomSessions = Array.from(players.values()).filter(p => String(p.gameId) === searchId);
    const aliveInRoom = roomSessions.filter(p => p.isAlive);
    
    console.log(`[CRITICAL-DEBUG] Sala: ${searchId}`);
    console.log(`[CRITICAL-DEBUG] Total en sala: ${roomSessions.length}`);
    console.log(`[CRITICAL-DEBUG] Vivos: ${aliveInRoom.length}`);
    roomSessions.forEach(p => console.log(`  - Jugador: ${p.playerId}, isAlive: ${p.isAlive}`));
    
    console.log(`[DEBUG-GAMEOVER] Sala: ${searchId}, Sesiones vinculadas: ${roomSessions.length}, Vivos: ${aliveInRoom.length}`);
    roomSessions.forEach(p => {
        console.log(`[DEBUG-GAMEOVER] - Jugador ${p.playerId} (Vivo: ${p.isAlive})`);
    });
    
    if (aliveInRoom.length === 0 && roomSessions.length > 0) {
        const room = waveManager.activeGames.get(searchId);
        
        // FAIL-SAFE: Si la sala no está en activeGames hoy, pero los jugadores están aquí, 
        // igual deberíamos intentar enviar el Game Over si no se ha enviado ya.
        if (!room) {
            console.log(`[DEBUG-GAMEOVER] Sala ${searchId} no encontrada en waveManager. ¿Ya finalizó?`);
            return;
        }

        if (room.gameOverTriggered) {
            console.log(`[DEBUG-GAMEOVER] Game Over ya disparado anteriormente para la sala ${searchId}.`);
            return;
        }
        room.gameOverTriggered = true;

        console.log(`[DEBUG-GAMEOVER] ¡DISPARANDO GAME_OVER PARA ${roomSessions.length} JUGADORES!`);
        
        const roomStats = roomSessions.map(p => ({
            userId: String(p.playerId),
            kills: p.kills,
            time: Math.floor((Date.now() - p.startTime) / 1000)
        }));

        const gameOverPayload = JSON.stringify({
            tipo: 'game_over',
            stats: roomStats
        });

        // Enviar a todos los clientes que pertenezcan a esta sala
        wss.clients.forEach(c => {
            if (String(c.gameId) === searchId && c.readyState === WebSocket.OPEN) {
                console.log(`[DEBUG-GAMEOVER] Enviando stats a: ${c.userId}`);
                c.send(gameOverPayload);
            }
        });

        // Detener lógica de hordas y marcar en DB
        waveManager.stopGame(searchId);
        
        const gameService = require('../services/GameService');
        gameService.finishGame(searchId).catch(err => console.error("[CRITICAL] Error en finishGame:", err));
    } else {
        console.log(`[DEBUG-GAMEOVER] Aún quedan ${aliveInRoom.length} jugadores vivos en la sala.`);
    }
}

module.exports = { handleShoot, handleImpact, handleDeath, players };
