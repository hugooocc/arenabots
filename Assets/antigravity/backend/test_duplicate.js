const mongoose = require('mongoose');
const User = require('./src/models/User');
const userService = require('./src/services/UserService');
require('dotenv').config();

const uri = process.env.MONGODB_URI || 'mongodb://localhost:27019/antigravity';

async function test() {
    try {
        await mongoose.connect(uri);
        console.log('Connected to:', uri);
        
        try {
            console.log('Attempting to register "hugo" again...');
            await userService.register('hugo', 'password123');
            console.log('SUCCESS (This is BAD, should have failed)');
        } catch (err) {
            console.log('FAILED as expected:', err.message);
        }
        
        process.exit(0);
    } catch (err) {
        console.error('Error:', err);
        process.exit(1);
    }
}

test();
