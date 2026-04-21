const mongoose = require('mongoose');
const userService = require('./src/services/UserService');
require('dotenv').config();

const uri = process.env.MONGODB_URI || 'mongodb://localhost:27019/antigravity';

async function test() {
    try {
        await mongoose.connect(uri);
        console.log('Connected to:', uri);
        
        try {
            console.log('Registering "PersistentUser" (mixed case)...');
            await userService.register('PersistentUser', 'pass123');
            console.log('Register OK');
        } catch (err) {
            console.log('Register skipped (likely exists):', err.message);
        }

        try {
            console.log('Logging in as "persistentuser" (lowercase)...');
            const result = await userService.login('persistentuser', 'pass123');
            console.log('Login OK:', result.user.username);
        } catch (err) {
            console.log('Login FAILED:', err.message);
        }

        try {
            console.log('Attempting to register "PERSISTENTUSER" (uppercase) again...');
            await userService.register('PERSISTENTUSER', 'pass123');
            console.log('Error: Second register succeeded (BAD)');
        } catch (err) {
            console.log('Second register FAILED as expected:', err.message);
        }
        
        process.exit(0);
    } catch (err) {
        console.error('Error:', err);
        process.exit(1);
    }
}

test();
