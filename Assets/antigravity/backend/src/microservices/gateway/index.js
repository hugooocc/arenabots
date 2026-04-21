require('dotenv').config();
const express = require('express');
const http = require('http');
const httpProxy = require('http-proxy');

const app = express();
const server = http.createServer(app);
const proxy = httpProxy.createProxyServer({});

const PORT = 3000;
const AUTH_URL = 'http://auth:3001';
const GAME_URL = 'http://game:3002';
const STATS_URL = 'http://stats:3003';

// Route API requests
app.all('/api/auth/*', (req, res) => {
    proxy.web(req, res, { target: AUTH_URL });
});

app.all('/api/users/*', (req, res) => {
    proxy.web(req, res, { target: STATS_URL });
});

app.all('/api/games/*', (req, res) => {
    proxy.web(req, res, { target: GAME_URL });
});

// Proxy WebSocket upgrades
server.on('upgrade', (req, socket, head) => {
    proxy.ws(req, socket, head, { target: GAME_URL });
});

proxy.on('error', (err, req, res) => {
    console.error('[Gateway] Proxy Error:', err.message);
    if (res && res.status) {
        res.status(500).send('Proxy Error');
    }
});

server.listen(PORT, () => {
    console.log(`[Gateway] Running on port ${PORT}`);
});
