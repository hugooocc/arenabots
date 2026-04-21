const PlayerSession = require('../src/models/PlayerSession');

describe('Server Validation Logic', () => {
    let session;

    beforeEach(() => {
        session = new PlayerSession('test_player');
    });

    test('T013: Rate limiting (max 10 shots per 1000ms)', () => {
        const now = Date.now();
        for (let i = 0; i < 10; i++) {
            expect(session.canFire(now + i)).toBe(true);
            session.addFireTimestamp(now + i);
        }
        // 11th shot within 1000ms should fail
        expect(session.canFire(now + 11)).toBe(false);
        // Shot after 1000ms should pass
        expect(session.canFire(now + 1001)).toBe(true);
    });

    test('T014: Vector normalization validation (magnitude 1.0 ±0.05)', () => {
        const validateVector = (x, y) => {
            const magnitude = Math.sqrt(x * x + y * y);
            return magnitude >= 0.95 && magnitude <= 1.05;
        };

        expect(validateVector(1.0, 0.0)).toBe(true);
        expect(validateVector(0.707, 0.707)).toBe(true); // ~sqrt(2)/2
        expect(validateVector(0.96, 0.0)).toBe(true);
        expect(validateVector(0.5, 0.5)).toBe(false);
        expect(validateVector(1.1, 0.0)).toBe(false);
    });

    test('T015: Idempotency (duplicate timestamps)', () => {
        const now = Date.now();
        expect(session.canFire(now)).toBe(true);
        session.addFireTimestamp(now);
        // Same timestamp should fail
        expect(session.canFire(now)).toBe(false);
    });

    test('Player state validation (isAlive)', () => {
        session.isAlive = false;
        expect(session.canFire(Date.now())).toBe(false);
    });
});
