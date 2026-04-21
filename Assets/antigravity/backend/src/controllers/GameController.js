const GameService = require('../services/GameService');

class GameController {
  async create(req, res) {
    try {
      const { name, maxPlayers, isPrivate } = req.body;
      const ownerId = req.user.id; 

      if (!name || name.trim().length < 3) {
          return res.status(400).json({ message: 'Nombre de sala inválido (mínimo 3 caracteres)' });
      }

      if (!maxPlayers || maxPlayers < 2 || maxPlayers > 8) {
          return res.status(400).json({ message: 'Número de jugadores inválido (2-8)' });
      }

      const game = await GameService.createGame(name, maxPlayers, ownerId, isPrivate);
      res.status(201).json({
        gameId: game._id,
        message: 'Partida creada',
        status: game.status,
        code: game.password
      });
    } catch (error) {
      console.error("[GameController] Error en create:", error);
      res.status(500).json({ message: error.message });
    }
  }

  async list(req, res) {
    try {
      const games = await GameService.getAvailableGames();
      const response = games.map(game => ({
        id: game._id,
        name: game.name,
        currentPlayers: game.players.length,
        maxPlayers: game.maxPlayers,
        status: game.status,
        isPrivate: !!game.password
      }));
      res.json(response);
    } catch (error) {
      res.status(500).json({ message: error.message });
    }
  }

  async join(req, res) {
    try {
      const { id } = req.params;
      const { password } = req.body;
      const userId = req.user.id;

      const game = await GameService.joinGame(id, userId, password);
      res.json({
        message: 'Te has unido a la partida',
        game: {
          id: game._id,
          players: game.players.map(p => p._id || p)
        }
      });
    } catch (error) {
      const status = (error.message === 'Game is full' || error.message === 'Invalid room password') ? 403 : 500;
      res.status(status).json({ message: error.message });
    }
  }

  async joinPrivate(req, res) {
    try {
      const { code } = req.body;
      const userId = req.user.id;

      if (!code) {
        return res.status(400).json({ message: 'Se requiere código de sala' });
      }

      const game = await GameService.joinGameByCode(code, userId);
      res.json({
        message: 'Te has unido a la partida',
        game: {
          id: game._id,
          players: game.players.map(p => p._id || p)
        }
      });
    } catch (error) {
      const status = (error.message === 'Game is full' || error.message === 'Sala no encontrada o ya ha comenzado') ? 403 : 500;
      res.status(status).json({ message: error.message });
    }
  }

  async start(req, res) {
    try {
      const { id } = req.params;
      const userId = req.user.id;

      const game = await GameService.startGame(id, userId);

      // Emit WS event to all players in the room
      const wss = req.app.get('wss'); // Need to set this in index.js
      if (wss) {
          const payload = JSON.stringify({ tipo: 'game_started', gameId: id });
          wss.clients.forEach(client => {
              if (client.readyState === 1 && client.gameId === id) {
                  client.send(payload);
              }
          });
      }

      res.json({
        message: 'Partida iniciada',
        status: game.status
      });
    } catch (error) {
      res.status(500).json({ message: error.message });
    }
  }
}

module.exports = new GameController();
