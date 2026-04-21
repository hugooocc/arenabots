const validateRegistration = (req, res, next) => {
    const { username, password } = req.body;

    if (!username || username.trim().length < 3) {
        return res.status(400).json({ 
            message: "Username must be at least 3 characters" 
        });
    }

    if (!password || password.length < 6) {
        return res.status(400).json({ 
            message: "Password must be at least 6 characters" 
        });
    }

    next();
};

const validateLogin = (req, res, next) => {
    const { username, password } = req.body;

    if (!username || !password) {
        return res.status(400).json({ 
            message: "Username and password are required" 
        });
    }

    next();
};

module.exports = {
    validateRegistration,
    validateLogin
};
