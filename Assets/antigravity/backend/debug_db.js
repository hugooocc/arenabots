const mongoose = require('mongoose');
const User = require('./src/models/User');
require('dotenv').config();

const uri = process.env.MONGODB_URI || 'mongodb://localhost:27019/antigravity';

async function verify() {
    try {
        await mongoose.connect(uri);
        console.log('Connected to:', uri);
        const users = await User.find({});
        console.log('Total users:', users.length);
        users.forEach(u => console.log(' - User:', u.username));
        process.exit(0);
    } catch (err) {
        console.error('Error:', err);
        process.exit(1);
    }
}

verify();
