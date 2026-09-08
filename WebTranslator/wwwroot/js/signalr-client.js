class SignalRClient {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.connectionId = null;

        this.handlers = {
            text: [],
            image: [],
            imageAnalysis: [],
            error: [],
            analysisError: [],
            saving: [],
            connection: []
        };

        this._reconnectAttempts = 0;
        this._maxReconnectAttempts = 5;
        this._reconnectTimer = null;
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
            this.connectionId =
                data?.sessionId ||
                this.connection.connectionId ||
                null;

            this._reconnectAttempts = 0;

            this.handlers.connection.forEach(callback => {
                callback(data);
            });
        });

        this.connection.on("TranslationStatus", (data) => {
            this.handlers.text.forEach(callback => {
                callback(data);
            });
        });

        this.connection.on("ImageTranslationStatus", (data) => {
            this.handlers.image.forEach(callback => {
                callback(data);
            });
        });

        this.connection.on("ImageAnalysisStatus", (data) => {
            this.handlers.imageAnalysis.forEach(callback => {
                callback(data);
            });
        });

        this.connection.on("AnalysisStatus", (data) => {
            this.handlers.imageAnalysis.forEach(callback => {
                callback(data);
            });
        });

        this.connection.on("SavingStatus", (data) => {
            this.handlers.saving.forEach(callback => {
                callback(data);
            });
            this.handlers.imageAnalysis.forEach(callback => {
                callback(data);
            });
        });

        this.connection.on("TranslationError", (error) => {
            this.handlers.error.forEach(callback => {
                callback(error);
            });
        });

        this.connection.on("AnalysisError", (error) => {
            this.handlers.analysisError.forEach(callback => {
                callback(error);
            });
        });

        this.connection.onreconnecting(() => {
            this.isConnected = false;
            this._reconnectAttempts++;
        });

        this.connection.onreconnected((connectionId) => {
            this.isConnected = true;
            this.connectionId = connectionId;
            this._reconnectAttempts = 0;

            this.handlers.connection.forEach(callback => {
                callback({
                    sessionId: connectionId,
                    reconnected: true
                });
            });
        });

        this.connection.onclose(() => {
            this.isConnected = false;
            this.connectionId = null;
        });
    }

    async startConnection() {
        if (!this.connection) {
            return;
        }

        if (
            this.connection.state === signalR.HubConnectionState.Connected ||
            this.connection.state === signalR.HubConnectionState.Connecting
        ) {
            return;
        }

        try {
            await this.connection.start();

            this.isConnected = true;
            this.connectionId = this.connection.connectionId;
            this._reconnectAttempts = 0;

            console.log("✅ SignalR connected");
        } catch (error) {
            this.isConnected = false;

            console.error(
                "❌ SignalR connection failed:",
                error
            );

            if (
                this._reconnectAttempts < this._maxReconnectAttempts
            ) {
                this._reconnectAttempts++;

                clearTimeout(this._reconnectTimer);

                this._reconnectTimer = setTimeout(() => {
                    this.startConnection();
                }, 5000);
            }
        }
    }

    onTextTranslation(callback) {
        if (typeof callback !== "function") {
            throw new TypeError("Callback must be a function");
        }

        this.handlers.text.push(callback);

        return () => {
            this.handlers.text =
                this.handlers.text.filter(cb => cb !== callback);
        };
    }

    onImageTranslation(callback) {
        if (typeof callback !== "function") {
            throw new TypeError("Callback must be a function");
        }

        this.handlers.image.push(callback);

        return () => {
            this.handlers.image =
                this.handlers.image.filter(cb => cb !== callback);
        };
    }

    onImageAnalysis(callback) {
        if (typeof callback !== "function") {
            throw new TypeError("Callback must be a function");
        }

        this.handlers.imageAnalysis.push(callback);

        return () => {
            this.handlers.imageAnalysis =
                this.handlers.imageAnalysis.filter(cb => cb !== callback);
        };
    }

    onError(callback) {
        if (typeof callback !== "function") {
            throw new TypeError("Callback must be a function");
        }

        this.handlers.error.push(callback);

        return () => {
            this.handlers.error =
                this.handlers.error.filter(cb => cb !== callback);
        };
    }

    onAnalysisError(callback) {
        if (typeof callback !== "function") {
            throw new TypeError("Callback must be a function");
        }

        this.handlers.analysisError.push(callback);

        return () => {
            this.handlers.analysisError =
                this.handlers.analysisError.filter(cb => cb !== callback);
        };
    }

    onSaving(callback) {
        if (typeof callback !== "function") {
            throw new TypeError("Callback must be a function");
        }

        this.handlers.saving.push(callback);

        return () => {
            this.handlers.saving =
                this.handlers.saving.filter(cb => cb !== callback);
        };
    }

    onConnected(callback) {
        if (typeof callback !== "function") {
            throw new TypeError("Callback must be a function");
        }

        this.handlers.connection.push(callback);

        return () => {
            this.handlers.connection =
                this.handlers.connection.filter(cb => cb !== callback);
        };
    }

    async invoke(method, ...args) {
        if (!this.connection) {
            throw new Error("Connection not initialized");
        }

        if (
            this.connection.state !==
            signalR.HubConnectionState.Connected
        ) {
            throw new Error("Connection is not connected");
        }

        return await this.connection.invoke(method, ...args);
    }

    getConnectionId() {
        return (
            this.connectionId ||
            this.connection?.connectionId ||
            null
        );
    }

    getState() {
        return (
            this.connection?.state ||
            signalR.HubConnectionState.Disconnected
        );
    }

    isConnectedState() {
        return (
            this.connection?.state ===
            signalR.HubConnectionState.Connected
        );
    }
}

window.signalRClient = new SignalRClient();