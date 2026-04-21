const mongoose = require('mongoose');
const userService = require('./src/services/UserService');
require('dotenv').config();

const uri = process.env.MONGODB_URI || 'mongodb://localhost:27019/antigravity';

async function test() {
    try {
        await mongoose.connect(uri);
        console.log('Connected to:', uri);
        
        try {
            console.log('Attempting to login as "hugo" with "password123"...');
            // Assuming the previously registered 'hugo' had 'password123' 
            // (I used it in my duplicate test which failed, but did I register it correctly before?)
            // Wait, I saw 'hugo' in the DB.
            const result = await userService.login('hugo', 'password123');
            console.log('LOGIN SUCCESS:', result.user.username);
        } catch (err) {
            console.log('LOGIN FAILED:', err.message);
        }
        
        process.exit(0);
    } catch (err) {
        console.error('Error:', err);
        process.exit(1);
    }
}

test();
