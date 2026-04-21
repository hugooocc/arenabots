FROM node:24-alpine

# Set the working directory directly
WORKDIR /usr/src/app

# Copy package.json and package-lock.json if available
COPY package*.json ./

# Install dependencies (only production deps if needed, but we install all for now)
RUN npm install

# Copy the rest of the backend code
COPY . .

# Expose the API and Websocket port
EXPOSE 3000

# Start the Node.js application
CMD ["node", "src/index.js"]
