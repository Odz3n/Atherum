class SignalRClient {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.handlers = {
            text: [],
            image: [],
            error: [],
            connection: []
        };
        this.connectionId = null;
        this._reconnectAttempts = 0;
        this._maxReconnectAttempts = 5;
    }

    initialize() {
        if (this.connection) {
            return this.connection;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/translationHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupConnectionEvents();
        this.startConnection();

        return this.connection;
    }

    setupConnectionEvents() {
        this.connection.on("Connected", (data) => {
            this.isConnected = true;
            this.connectionId = data?.sessionId || this.connection.connectionId;
            this._reconnectAttempts = 0;
            this.handlers.connection.forEach(callback => callback(data));
        });

        this.connection.on("TranslationStatus", (data) => {
            this.handlers.text.forEach(callback => callback(data));
        });

        this.connection.on("ImageTranslationStatus", (data) => {
            this.handlers.image.forEach(callback => callback(data));
        });

        this.connection.on("TranslationError", (error) => {
            this.handlers.error.forEach(callback => callback(error));
        });

        this.connection.onreconnecting(() => {
            this.isConnected = false;
            this._reconnectAttempts++;
        });

        this.connection.onreconnected((connectionId) => {
            this.isConnected = true;
            this.connectionId = connectionId;
            this._reconnectAttempts = 0;
        });

        this.connection.onclose(() => {
            this.isConnected = false;
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log("✅ SignalR connected");
        } catch (error) {
            console.error("❌ SignalR connection failed:", error);
            if (this._reconnectAttempts < this._maxReconnectAttempts) {
                setTimeout(() => this.startConnection(), 5000);
            }
        }
    }

    onTextTranslation(callback) {
        this.handlers.text.push(callback);
        return () => {
            this.handlers.text = this.handlers.text.filter(cb => cb !== callback);
        };
    }

    onImageTranslation(callback) {
        this.handlers.image.push(callback);
        return () => {
            this.handlers.image = this.handlers.image.filter(cb => cb !== callback);
        };
    }

    onError(callback) {
        this.handlers.error.push(callback);
        return () => {
            this.handlers.error = this.handlers.error.filter(cb => cb !== callback);
        };
    }

    onConnected(callback) {
        this.handlers.connection.push(callback);
        return () => {
            this.handlers.connection = this.handlers.connection.filter(cb => cb !== callback);
        };
    }

    async invoke(method, ...args) {
        if (!this.connection) {
            throw new Error("Connection not initialized");
        }
        if (!this.isConnected) {
            throw new Error("Connection is not connected");
        }
        return await this.connection.invoke(method, ...args);
    }

    getConnectionId() {
        return this.connectionId || this.connection?.connectionId || null;
    }

    getState() {
        return this.connection?.state || signalR.HubConnectionState.Disconnected;
    }

    isConnectedState() {
        return this.isConnected && this.connection?.state === signalR.HubConnectionState.Connected;
    }
}

window.signalRClient = new SignalRClient();