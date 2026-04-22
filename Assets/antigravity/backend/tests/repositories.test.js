const userRepository = require('../src/repositories/inmemory/UserRepository');
const gameRepository = require('../src/repositories/inmemory/GameRepository');

describe('InMemory Repositories', () => {
    describe('UserRepository', () => {
        it('should create and find a user', async () => {
            const userData = { username: 'testuser', password: 'password123' };
            const created = await userRepository.createUser(userData);
            expect(created.username).toBe('testuser');
            
            const found = await userRepository.findUserByUsername('testuser');
            expect(found.id).toBe(created.id);
        });

        it('should update user stats', async () => {
            const user = await userRepository.createUser({ username: 'statuser' });
            const stats = await userRepository.updateStats(user.id, 10, 60);
            expect(stats.mobsKilled).toBe(10);
            expect(stats.timeSurvived).toBe(60);
        });

        it('should return all users sorted by mobsKilled', async () => {
            // Clear or use fresh repository (InMemory is singleton here, so handle existing)
            await userRepository.createUser({ username: 'user_A' });
            const uB = await userRepository.createUser({ username: 'user_B' });
            const uC = await userRepository.createUser({ username: 'user_C' });

            await userRepository.updateStats(uB.id, 50, 100);
            await userRepository.updateStats(uC.id, 100, 200);

            const ranking = await userRepository.getAllUsersSorted(5);
            expect(ranking[0].username).toBe('user_C');
            expect(ranking[0].maxMobsKilled).toBe(100);
            expect(ranking[1].username).toBe('user_B');
            expect(ranking[1].maxMobsKilled).toBe(50);
        });
    });

    describe('GameRepository', () => {
        it('should create and join a game', async () => {
            const gameData = { name: 'Test Room', maxPlayers: 4 };
            const created = await gameRepository.createGame(gameData);
            expect(created.name).toBe('Test Room');

            await gameRepository.joinGame(created.id, 'user1');
            const found = await gameRepository.findById(created.id);
            expect(found.players).toContain('user1');
        });

        it('should update game status', async () => {
            const game = await gameRepository.createGame({ name: 'Status Room' });
            await gameRepository.updateStatus(game.id, 'IN_PROGRESS');
            const found = await gameRepository.findById(game.id);
            expect(found.status).toBe('IN_PROGRESS');
        });
    });
});
