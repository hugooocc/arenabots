const { spawn } = require('child_process');
const WebSocket = require('ws');

const server = spawn('node', ['src/microservices/game/index.js'], { 
    cwd: 'c:\\Users\\HUGO06\\Desktop\\REPOS PERSONALES GITHUB\\ACTIVIDADES PSP\\EXAMEN-VALIDACIÓ-UNITY\\ARENA BOTS\\Assets\\antigravity\\backend',
    env: { ...process.env, PORT: '3007' }
});

server.stdout.on('data', (d) => console.log('[SERVER]', d.toString().trim()));
server.stderr.on('data', (d) => console.log('[SERVER-ERR]', d.toString().trim()));

setTimeout(() => {
    console.log('--- Conectando Cliente 1 ---');
    const ws1 = new WebSocket('ws://localhost:3007/?gameId=TEST1234');
    
    ws1.on('open', () => {
        console.log('WS1 Open');
        ws1.send(JSON.stringify({ tipo: 'player_ready', username: 'Hugo' }));
        
        console.log('--- Conectando Cliente 2 ---');
        const ws2 = new WebSocket('ws://localhost:3007/?gameId=TEST1234');
        
        ws2.on('open', () => {
            console.log('WS2 Open');
            ws2.send(JSON.stringify({ tipo: 'player_ready', username: 'Amigo' }));
            
            setTimeout(() => {
                console.log('--- Cliente 1 muere ---');
                ws1.send(JSON.stringify({ tipo: 'player_dead', partidaId: 'TEST1234' }));
                
                setTimeout(() => {
                    console.log('--- Cliente 2 muere ---');
                    ws2.send(JSON.stringify({ tipo: 'player_dead', partidaId: 'TEST1234' }));
                    
                    setTimeout(() => {
                        console.log('KIlling server');
                        server.kill();
                        process.exit(0);
                    }, 1000);
                }, 500);
            }, 1000);
        });

        ws2.on('message', m => {
            const data = JSON.parse(m.toString());
            if (data.tipo !== 'game_tick' && data.tipo !== 'nuevo_jugador') {
                console.log('[WS2 Msg]', data.tipo);
            }
        });
    });

    ws1.on('message', m => {
        const data = JSON.parse(m.toString());
        if (data.tipo !== 'game_tick' && data.tipo !== 'nuevo_jugador') {
            console.log('[WS1 Msg]', data.tipo);
        }
    });

}, 2000); // give server time to start
